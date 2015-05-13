using System;
using System.Collections.Generic;
using System.Linq;
using myOrbwalker;
using LeagueSharp.Common.Data;
using LeagueSharp.Common;
using LeagueSharp;
using SharpDX;
using Color = System.Drawing.Color;


namespace Volibear
{
    class Program
    {
        private const string ChampionName = "Volibear";
        private static Menu Config;
        private static Spell Q, W, E, R;
        private static Obj_AI_Hero lastTarget;  
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName != ChampionName) return;
            Q = new Spell(SpellSlot.Q);           
            W = new Spell(SpellSlot.W, 400);
            E = new Spell(SpellSlot.E, 425);
            R = new Spell(SpellSlot.R);

            Q.SetTargetted(0.5f, 100);

            Config = new Menu("Volibear", "Volibear", true);
            var orbwalkerMenu = new Menu("Orbwalker", "Orbwalker");
            MYOrbwalker.AddToMenu(orbwalkerMenu);
            Config.AddSubMenu(orbwalkerMenu);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.AddSubMenu(new Menu("Keys", "Keys"));
            Config.SubMenu("Keys").AddItem(new MenuItem("ComboActive", "Combo").SetValue(new KeyBind(Config.Item("Combo_Key").GetValue<KeyBind>().Key, KeyBindType.Press))); //x
            Config.SubMenu("Keys").AddItem(new MenuItem("HarassActive", "Harass").SetValue(new KeyBind(Config.Item("Harass_Key").GetValue<KeyBind>().Key, KeyBindType.Press))); //s
            Config.SubMenu("Keys").AddItem(new MenuItem("LastHitActive", "Last Hit").SetValue(new KeyBind(Config.Item("LastHit_Key").GetValue<KeyBind>().Key, KeyBindType.Press))); //a
            Config.SubMenu("Keys").AddItem(new MenuItem("LaneClearActive", "Lane Clear").SetValue(new KeyBind(Config.Item("LaneClear_Key").GetValue<KeyBind>().Key, KeyBindType.Press))); //c
            Config.SubMenu("Keys").AddItem(new MenuItem("FreezeActive", "Lane Freeze").SetValue(new KeyBind(Config.Item("LaneFreeze_Key").GetValue<KeyBind>().Key, KeyBindType.Press))); //z
            Config.SubMenu("Keys").AddItem(new MenuItem("JungleActive", "Jungle Clear").SetValue(new KeyBind(Config.Item("JungleClear_Key").GetValue<KeyBind>().Key, KeyBindType.Press))); //v
            Config.SubMenu("Keys").AddItem(new MenuItem("FleeActive", "Flee").SetValue(new KeyBind(Config.Item("Flee_Key").GetValue<KeyBind>().Key, KeyBindType.Press)));  //space
            
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(false));
            Config.SubMenu("Combo").AddItem(new MenuItem("TurretDive", "Turret Dive").SetValue(false));
            Config.SubMenu("Combo").AddItem(new MenuItem("Sticky", "Stick to Target").SetValue(false));
            Config.SubMenu("Combo").AddItem(new MenuItem("StickyType", "Type").SetValue(new StringList(new[] { "AA Range", "Slider" })));
            Config.SubMenu("Combo").AddItem(new MenuItem("StickyTypeSlider", "Range").SetValue(new Slider(50, 50, 1000)));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseItemCombo", "Use Items").SetValue(true));
            
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            
            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseWFarm", "Use W").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseEFarm", "Use E").SetValue(true));            
            Config.SubMenu("Farm").AddItem(new MenuItem("WFarmType", "W").SetValue(new StringList(new[] { "Any", "Only Siege", "Low HP" })));
            Config.SubMenu("Farm").AddItem(new MenuItem("WFarmTypeHP", "(Low HP) HP <").SetValue(new Slider(70)));
            Config.SubMenu("Farm").AddItem(new MenuItem("EFarmValue", "E More Than").SetValue(new Slider(1, 1, 5)));
            Config.SubMenu("Farm").AddItem(new MenuItem("FarmMana", "Farm Mana >").SetValue(new Slider(50)));

            Config.AddSubMenu(new Menu("Jungle Farm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseWJFarm", "Use W").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(true));

            Config.AddToMainMenu();
            Game.OnUpdate += Game_OnUpdate;
            MYOrbwalker.AfterAttack += OnAfterAttack;
            MYOrbwalker.BeforeAttack += BeforeAttack;
            MYOrbwalker.OnNonKillableMinion += OnNonKillableMinion;
            Drawing.OnEndScene += Drawing_OnEndScene;
        }
        private static void OnNonKillableMinion(AttackableUnit minion)
        {
            if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.LaneClear)
            {
                if (Config.Item("UseWFarm").GetValue<bool>() && Config.Item("WFarmType").GetValue<StringList>().SelectedIndex == 0 &&
                    GetPlayerManaPercentage() > Config.Item("FarmMana").GetValue<Slider>().Value)
                {
                    var target = minion as Obj_AI_Minion;
                    if (target != null && W.IsKillable(target) && W.IsReady() && WBuff())
                    {
                        W.Cast(target);
                    }
                }
            }
        }
        private static void BeforeAttack(MYOrbwalker.BeforeAttackEventArgs args)
        {
            if (args.Target is Obj_AI_Hero)
            {
                if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Combo)
                {
                    if (W.IsKillable(args.Target) && W.IsReady() && WBuff() && Config.Item("UseWCombo").GetValue<bool>())
                    {
                        W.Cast(args.Target);
                    }
                    if (R.IsReady() && !ObjectManager.Player.IsWindingUp && Config.Item("UseRCombo").GetValue<bool>() && Orbwalking.InAutoAttackRange(args.Target) && !QBuff())
                    {
                        R.Cast();
                    }
                }
            }
        }
        private static void OnAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe) return;
            if (unit.IsMe)
            {
                if (!ObjectManager.Player.IsWindingUp &&
                    (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Combo && Config.Item("UseItemCombo").GetValue<bool>()) &&
                    Orbwalking.InAutoAttackRange(target))
                {
                    UseItems(2, null);
                }
            }
        }
        private static void Drawing_OnEndScene(EventArgs args)
        {
            if (ObjectManager.Player.IsDead) return;
            var target = MYOrbwalker.ComboLocked ? LockedTargetSelector._lastTarget : MYOrbwalker.GetEnemyChampion();
            if (target != null) Render.Circle.DrawCircle(target.Position, 125, Color.Lime, 7, true);

        }
        private static void Game_OnUpdate(EventArgs args)
        {
            if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.None)
            {
                LockedTargetSelector.UnlockTarget();
                SetOrbwalkToDefault();
            }
            if (ObjectManager.Player.IsDead) return;
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active) Combo();
            if (Config.Item("HarassActive").GetValue<KeyBind>().Active) Harass();
            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active) LaneClear();                
            if (Config.Item("JungleActive").GetValue<KeyBind>().Active) JungleClear();            
        }
        private static void Combo()
        {
            //var target = LockedTargetSelector.GetTarget(1000f, TargetSelector.DamageType.Physical);
            Obj_AI_Hero target;
            if (MYOrbwalker.ComboLocked)
            {
                target = LockedTargetSelector.GetTarget(1000f, TargetSelector.DamageType.Physical);
            }
            else
            {
                target = MYOrbwalker.GetEnemyChampion();
            }
            var UseQ = Config.Item("UseQCombo").GetValue<bool>();
            var UseW = Config.Item("UseWCombo").GetValue<bool>();
            var UseE = Config.Item("UseECombo").GetValue<bool>();            
            var Sticks = Config.Item("Sticky").GetValue<bool>();
            var CastItems = Config.Item("UseItemCombo").GetValue<bool>();
            if (Sticks)
            {
                if (target.IsValidTarget()) SetOrbwalkingToTarget(target);
            }
            if (target.IsValidTarget())
            {
                if (target.InFountain()) return;
                if (CastItems) { UseItems(0, target); }
                try
                {
                    if (UseQ && Q.IsReady() && ObjectManager.Player.IsFacing(target) && Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) < 800f)
                    {
                        if (target.UnderTurret(true) && !Config.Item("TurretDive").GetValue<bool>()) return;
                        if (target.UnderTurret(true) && Config.Item("TurretDive").GetValue<bool>() && !Passive()) return;
                        Q.CastOnUnit(ObjectManager.Player);
                    }
                    if (UseW && W.IsReady() && W.IsInRange(target) && WBuff() && !QBuff())
                    {
                        W.Cast(target);
                    }
                    if (UseE && E.IsReady() && Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) < 425f && Orbwalking.InAutoAttackRange(target) && !QBuff())
                    {
                        E.Cast();
                    }
                }
                catch { }
            }
            else SetOrbwalkToDefault();
        }
        private static void Harass()
        { 
            var UseQ = Config.Item("UseQHarass").GetValue<bool>();
            var UseW = Config.Item("UseWHarass").GetValue<bool>();
            var UseE = Config.Item("UseEHarass").GetValue<bool>();
            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
            if (target.IsValidTarget())
            {
                if (ObjectManager.Player.UnderTurret(true) && target.UnderTurret(true)) return;
                if (UseQ && Q.IsReady() && Orbwalking.InAutoAttackRange(target))
                {
                    Q.CastOnUnit(ObjectManager.Player);
                }
                if (UseW && W.IsReady() && W.IsInRange(target) && WBuff() && !QBuff())
                {
                    W.Cast(target);
                }
                if (UseE && E.IsReady() && Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) < 425f && !Orbwalking.InAutoAttackRange(target) && !QBuff())
                {
                    E.Cast();
                }
            }
        }
        private static void LaneClear()
        {
            if (GetPlayerManaPercentage() < Config.Item("FarmMana").GetValue<Slider>().Value) return;
            if (Config.Item("UseWFarm").GetValue<bool>() && W.IsReady())
            {
                switch (Config.Item("WFarmType").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        var minionsW = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).Where(x => W.IsKillable(x)).OrderBy(d => d.Distance(ObjectManager.Player)).FirstOrDefault();
                        if (minionsW.IsValidTarget() && !ObjectManager.Player.IsWindingUp)
                        {
                            W.Cast(minionsW);
                        }
                        break;
                    case 1:
                        var siegeW = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).FirstOrDefault(x => x.BaseSkinName.Contains("Siege") && W.IsKillable(x));
                        Q.Cast(siegeW);
                        break;
                    case 2:
                        if (GetPlayerHealthPercentage() < Config.Item("WFarmTypeHP").GetValue<Slider>().Value & !ObjectManager.Player.IsWindingUp)
                        {
                            var anyW = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth).Where(x => !W.IsKillable(x)).OrderBy(d => d.Distance(ObjectManager.Player)).FirstOrDefault();
                            W.Cast(anyW);
                        }
                        break;
                }
            }
            if (Config.Item("UseEFarm").GetValue<bool>() && E.IsReady())
            {
                var minionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition ,E.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.None);
                if (minionsE.Count > Config.Item("EFarmValue").GetValue<Slider>().Value && MYOrbwalker.IsWaiting() && !ObjectManager.Player.IsWindingUp)
                {
                    E.Cast();
                }
            }
        }
        private static void JungleClear()
        {
            
            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            var largemobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.Health).FirstOrDefault(x => !x.BaseSkinName.Contains("Mini"));
            if (mobs.Count <= 0) return;
            var mob = mobs[0];
            if (mob != null)
            {
                if (Config.Item("UseWJFarm").GetValue<bool>() && W.IsReady() && WBuff())
                {
                    if (largemobs != null)
                    {
                        if (W.IsKillable(largemobs))
                        {
                            W.Cast(largemobs);
                        }
                        W.Cast(mob);
                    }
                    else
                    {
                        W.Cast(mob);
                    }
                }
                if (Config.Item("UseEJFarm").GetValue<bool>() && E.IsReady())
                {
                    var mobsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, ObjectManager.Player.AttackRange, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                    if (mobsE.Count > 0 && !ObjectManager.Player.IsWindingUp)
                    {
                        E.Cast();
                    }
                }
            }
        }
        private static void UseItems(int type, Obj_AI_Base target)
        {
            Int16[] SelfItems = { 3142 }; //Just Ghostblade //3180, 3131, 3074, 3077, 
            Int16[] TargetingItems = { 3153, 3144 }; //Just botrk //3188, 3128, 3146, 3184 };
            Int16[] AoeItems = { 3143 }; //Randuin
            switch (type)
            {
                case 0:
                    foreach (var itemId in SelfItems.Where(itemId => Items.HasItem(itemId) && Items.CanUseItem(itemId)))
                    {
                        Items.UseItem(itemId);
                    }
                    break;
                case 1:
                    foreach (var itemId in TargetingItems.Where(itemId => Items.HasItem(itemId) && Items.CanUseItem(itemId)))
                    {
                        if (target != null) Items.UseItem(itemId, target);
                    }
                    break;
                case 2:
                    foreach (var itemId in AoeItems.Where(itemId => Items.HasItem(itemId) && Items.CanUseItem(itemId)))
                    {
                        Items.UseItem(itemId);
                    }
                    break;
            } 
        }
        private static float GetPlayerHealthPercentage()
        {
            return ObjectManager.Player.Health * 100 / ObjectManager.Player.MaxHealth;
        }
        private static float GetPlayerManaPercentage()
        {
            return ObjectManager.Player.Mana * 100 / ObjectManager.Player.MaxMana;
        }       
        private static void SetOrbwalkToDefault()
        {
            MYOrbwalker.SetMovement(true);
            MYOrbwalker.SetOrbwalkingPoint(new Vector3());
        }
        private static void SetOrbwalkingToTarget(Obj_AI_Hero target)
        {
            if (!target.IsValidTarget() || target.IsDead || target.IsZombie || target.ServerPosition.UnderTurret(true))
            {
                SetOrbwalkToDefault();
            }
            if (!lastTarget.IsValidTarget() || lastTarget.IsDead || lastTarget.IsZombie)
            {
                SetOrbwalkToDefault();
            }
            if (lastTarget == null || !lastTarget.IsValidTarget())
            {
                if (target.IsValidTarget()) lastTarget = target;
            }
            if (target == lastTarget)
            {
                switch (Config.Item("StickyType").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        if (Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) < ObjectManager.Player.AttackRange)
                        {
                            MYOrbwalker.SetMovement(false);
                            if (!ObjectManager.Player.IsWindingUp) MYOrbwalker.SetOrbwalkingPoint(ObjectManager.Player.ServerPosition.Shorten(target.ServerPosition, 10f));
                        }
                        else SetOrbwalkToDefault();
                        break;
                    case 1:
                        if (Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) < Config.Item("StickyTypeSlider").GetValue<Slider>().Value)
                        {
                            MYOrbwalker.SetMovement(false);
                            if (!ObjectManager.Player.IsWindingUp) MYOrbwalker.SetOrbwalkingPoint(ObjectManager.Player.ServerPosition.Shorten(target.ServerPosition, 10f));
                        }
                        else SetOrbwalkToDefault();
                        break;
                }
            }
            else SetOrbwalkToDefault();
        }
        private static bool QBuff()
        {
            return ObjectManager.Player.HasBuff("VolibearQ");
        }
        private static bool WBuff()
        {
            return ObjectManager.Player.HasBuff("volibearwparticle");
        }
        private static bool Passive()
        {
            return ObjectManager.Player.HasBuff("volibearpassivebuff");
        }
    }
}
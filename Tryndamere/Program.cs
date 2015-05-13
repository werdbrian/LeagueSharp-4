using System;
using System.Linq;
using myOrbwalker;
using LeagueSharp.Common;
using LeagueSharp;
using SharpDX;
using Color = System.Drawing.Color;


namespace Tryndamere
{
    class Program
    {
        private const string ChampionName = "Tryndamere";
        private static Menu Config;
        private static Spell Q, W, E, R, QSharp;
        private static Obj_AI_Hero lastTarget;        
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName != ChampionName) return;
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 400f);
            E = new Spell(SpellSlot.E, 600f);
            R = new Spell(SpellSlot.R);
            
            W.SetSkillshot(W.Instance.SData.SpellCastTime, W.Instance.SData.LineWidth, W.Instance.SData.MissileSpeed, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(E.Instance.SData.SpellCastTime, E.Instance.SData.LineWidth, E.Instance.SData.MissileSpeed, false, SkillshotType.SkillshotLine);

            Config = new Menu("Tryndamere", "Tryndamere", true);
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
            Config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(false));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseEComboValue", "E Extend").SetValue(new Slider(50, 0, 600)));
            Config.SubMenu("Combo").AddItem(new MenuItem("TurretDive", "Turret Dive").SetValue(false));
            Config.SubMenu("Combo").AddItem(new MenuItem("MustHaveR", "Have R (Turret Dive)").SetValue(false));
            Config.SubMenu("Combo").AddItem(new MenuItem("Sticky", "Stick to Target").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("StickyType", "Type").SetValue(new StringList(new[] { "AA Range", "Slider" })));
            Config.SubMenu("Combo").AddItem(new MenuItem("StickyTypeSlider", "Range").SetValue(new Slider(50, 50, 1000)));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseItemCombo", "Use Items").SetValue(true));
            
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarassType", "W Type").SetValue(new StringList(new[] { "Any", "Not Facing" })));
            
            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseEFarm", "Use E").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("EFarmType", "E").SetValue(new StringList(new[] { "Any", "Furthest" })));
            Config.SubMenu("Farm").AddItem(new MenuItem("EFarmValue", "(Any) E More Than").SetValue(new Slider(1, 1, 5)));

            Config.AddSubMenu(new Menu("Jungle Farm", "JungleFarm"));            
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(true));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("DrawTarget", "Circle Target").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("MiscQ", "Auto Q").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("MiscQHP", "Keep HP >").SetValue(new Slider(50)));
            Config.SubMenu("Misc").AddItem(new MenuItem("MiscQCheckR", "Check R").SetValue(true));

            Config.AddToMainMenu();
            MYOrbwalker.AfterAttack += OnAfterAttack;
            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnEndScene += Drawing_OnEndScene;

        }
        private static void Drawing_OnEndScene(EventArgs args)
        {
            if (ObjectManager.Player.IsDead) return;
            if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Combo && Config.Item("DrawTarget").GetValue<bool>())
            {
                var target = MYOrbwalker.ComboLocked ? LockedTargetSelector._lastTarget : MYOrbwalker.GetEnemyChampion();
                if (target != null) Render.Circle.DrawCircle(target.Position, 125, Color.Lime, 7, true);
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
        private static void Game_OnUpdate(EventArgs args)
        {
            if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.None)
            {
                LockedTargetSelector.UnlockTarget();
                SetOrbwalkToDefault();
            }
            if (ObjectManager.Player.IsDead) return;
            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active) LaneClear();
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active) Combo();
            if (Config.Item("HarassActive").GetValue<KeyBind>().Active) Harass(); 
            if (Config.Item("JungleActive").GetValue<KeyBind>().Active) JungleClear();
            SmartQ();            
        }
        private static void LaneClear()
        {
            if (Config.Item("UseEFarm").GetValue<bool>() && E.IsReady() && !ObjectManager.Player.IsWindingUp)
            {
                var AllMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Enemy);
                switch (Config.Item("EFarmType").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        //Any
                        MinionManager.FarmLocation ELine = E.GetLineFarmLocation(AllMinionsE);
                        if (ELine.Position.IsValid() && !ELine.Position.To3D().UnderTurret(true) && Vector3.Distance(ObjectManager.Player.ServerPosition, ELine.Position.To3D()) > ObjectManager.Player.AttackRange)
                        {
                            if (ELine.MinionsHit > Config.Item("EFarmValue").GetValue<Slider>().Value) E.Cast(ELine.Position);
                        }
                        break;
                    case 1:
                        //Furthest
                        var FurthestE = AllMinionsE.OrderByDescending(i => i.Distance(ObjectManager.Player)).FirstOrDefault(x => !x.UnderTurret(true));
                        if (FurthestE != null && FurthestE.Position.IsValid() && !Orbwalking.InAutoAttackRange(FurthestE))
                        {
                            E.Cast(FurthestE.Position);
                        }
                        break;
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
            if (Config.Item("UseEJFarm").GetValue<bool>() && E.IsReady() && !ObjectManager.Player.IsWindingUp)
            {
                if (largemobs != null)
                {
                    E.Cast(ObjectManager.Player.ServerPosition.Extend(largemobs.ServerPosition, Vector3.Distance(ObjectManager.Player.ServerPosition, largemobs.ServerPosition) + 75f));
                }
                else
                {
                    E.Cast(ObjectManager.Player.ServerPosition.Extend(mob.ServerPosition, Vector3.Distance(ObjectManager.Player.ServerPosition, mob.ServerPosition) + 75f));
                }
            }
        }
        private static void Combo()
        {
            Obj_AI_Hero target;
            if (MYOrbwalker.ComboLocked)
            {
                target = LockedTargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
            }
            else
            {
                target = MYOrbwalker.GetEnemyChampion();
            }
            var UseW = Config.Item("UseWCombo").GetValue<bool>();
            var UseE = Config.Item("UseECombo").GetValue<bool>();
            var UseR = Config.Item("UseRCombo").GetValue<bool>();
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
                    if (UseW && W.IsReady() && !ObjectManager.Player.IsWindingUp && !ObjectManager.Player.IsDashing())
                    {
                        W.Cast();
                    }
                    if (UseE && E.IsReady() && !ObjectManager.Player.IsWindingUp && Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) < E.Range)
                    {
                        if (target.ServerPosition.UnderTurret(true) && !Config.Item("TurretDive").GetValue<bool>()) return;
                        E.Cast(ObjectManager.Player.ServerPosition.Extend(target.ServerPosition, Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) + Config.Item("UseEComboValue").GetValue<Slider>().Value));
                    }
                    if (UseR && R.IsReady() && !ObjectManager.Player.IsWindingUp)
                    {
                       if (GetPlayerHealthPercentage() < 20)
                    {
                        R.Cast();
                    }
                    }
                    if (Orbwalking.InAutoAttackRange(target) && CastItems && !ObjectManager.Player.IsWindingUp)
                    {
                        UseItems(1, target);
                    }
                }
                catch { }
            }
            else SetOrbwalkToDefault();
        }
        private static void Harass()
        {
            var UseW = Config.Item("UseWHarass").GetValue<bool>();
            var UseE = Config.Item("UseEHarass").GetValue<bool>();
            
            if (UseW && W.IsReady() && !ObjectManager.Player.IsWindingUp && !ObjectManager.Player.IsDashing())
            {
                var ValidW = ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy && !enemy.IsDead && enemy.IsValidTarget() && Vector3.Distance(ObjectManager.Player.ServerPosition, enemy.ServerPosition) < W.Range).OrderBy(i => i.Distance(ObjectManager.Player)).ToList();
                switch (Config.Item("UseWHarassType").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        if (ValidW.Any()) W.Cast();
                        break;
                    case 1:
                        if (ValidW.Any(x => !x.IsFacing(ObjectManager.Player))) W.Cast();
                        break;
                }
            }
            if (UseE && E.IsReady() && !ObjectManager.Player.IsWindingUp)
            {
                var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                if (target.UnderTurret(true)) return;
                if (ObjectManager.Player.ServerPosition.Extend(target.ServerPosition, 100f).UnderTurret(true)) return;
                if (Orbwalking.InAutoAttackRange(target)) return;
                E.Cast(ObjectManager.Player.ServerPosition.Extend(target.ServerPosition, Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) + 75f));
            }
        }
        private static float GetPlayerHealthPercentage()
        {
            return ObjectManager.Player.Health * 100 / ObjectManager.Player.MaxHealth;
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
        private static bool RBuff()
        {
            return ObjectManager.Player.HasBuff("UndyingRage");
        }
      
        private static void SmartQ()
        {
            if (MYOrbwalker.CurrentMode != MYOrbwalker.Mode.None) return;
            if (ObjectManager.Player.InFountain() || ObjectManager.Player.InShop() || ObjectManager.Player.HasBuff("Recall")) return;
            if (Config.Item("MiscQ").GetValue<bool>() && Q.IsReady())
            {
                if (GetPlayerHealthPercentage() < Config.Item("MiscQHP").GetValue<Slider>().Value)
                {
                    if (RBuff() && Config.Item("MiscQCheckR").GetValue<bool>()) return;
                    Q.Cast();
                }
              
            }
        }     
        private static void UseItems(int type, Obj_AI_Base target)
        {
            Int16[] SelfItems = { 3142 }; //3180, 3131, 3074, 3077, 
            Int16[] TargetingItems = { 3153, 3144, 3188, 3128, 3146, 3184 };
            Int16[] AoeItems = { 3074, 3077 }; //3180, 3131, 3074, 3077,             
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
    }
}

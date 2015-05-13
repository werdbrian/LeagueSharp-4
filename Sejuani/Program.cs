using System;
using System.Collections.Generic;
using System.Linq;
using myOrbwalker;
using LeagueSharp.Common.Data;
using LeagueSharp.Common;
using LeagueSharp;
using SharpDX;
using Color = System.Drawing.Color;


namespace Sejuani
{
    class Program
    {
        private const string ChampionName = "Sejuani";
        private static Menu Config;
        private static Spell Q,Q2, W, E, R;
        private static Obj_AI_Hero lastTarget;    
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName != ChampionName) return;
            Q = new Spell(SpellSlot.Q, 650);
            Q2 = new Spell(SpellSlot.Q, 650);
            W = new Spell(SpellSlot.W, 350);
            E = new Spell(SpellSlot.E, 1000);
            R = new Spell(SpellSlot.R, 1175);

            Q.SetSkillshot(Q.Instance.SData.SpellCastTime, Q.Instance.SData.LineWidth, Q.Instance.SData.MissileSpeed, true, SkillshotType.SkillshotLine);
            Q2.SetSkillshot(Q.Instance.SData.SpellCastTime, Q.Instance.SData.LineWidth, Q.Instance.SData.MissileSpeed, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(250, 110, 1600, false, SkillshotType.SkillshotLine);

            Config = new Menu("Sejuani", "Sejuani", true);
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
            Config.SubMenu("Combo").AddItem(new MenuItem("Sticky", "Stick to Target").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("StickyType", "Type").SetValue(new StringList(new[] { "AA Range", "Slider" })));
            Config.SubMenu("Combo").AddItem(new MenuItem("StickyTypeSlider", "Range").SetValue(new Slider(50, 50, 1000)));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseItemCombo", "Use Items").SetValue(true));
            
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            
            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(false));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseWFarm", "Use W").SetValue(false));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseEFarm", "Use E").SetValue(false));
            Config.SubMenu("Farm").AddItem(new MenuItem("QFarmType", "Q").SetValue(new StringList(new[] { "Any", "Furthest" })));
            Config.SubMenu("Farm").AddItem(new MenuItem("QFarmValue", "(Any) Q More Than").SetValue(new Slider(1, 1, 5)));
            Config.SubMenu("Farm").AddItem(new MenuItem("EFarmType", "E").SetValue(new StringList(new[] { "Any", "Most", "Only Siege" })));
            Config.SubMenu("Farm").AddItem(new MenuItem("FarmMana", "Farm Mana >").SetValue(new Slider(50)));

            Config.AddSubMenu(new Menu("Jungle Farm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseWJFarm", "Use W").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(true));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("QPredHitchance", "Q Hitchance").SetValue(new StringList(new[] { "Low", "Medium", "High" })));

            Config.AddToMainMenu();
            Game.OnUpdate += Game_OnUpdate;
            MYOrbwalker.AfterAttack += OnAfterAttack;
            //MYOrbwalker.BeforeAttack += BeforeAttack;
            Drawing.OnEndScene += Drawing_OnEndScene;
        }
        private static void BeforeAttack(MYOrbwalker.BeforeAttackEventArgs args)
        {
            if (args.Target is Obj_AI_Hero)
            {
                if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
                {
                    if (W.IsReady() && !ObjectManager.Player.IsWindingUp && Config.Item("UseWCombo").GetValue<bool>() && Orbwalking.InAutoAttackRange(args.Target))
                    {
                        Utility.DelayAction.Add(200, () => W.Cast());                  
                    }
                }
            }
        }
        private static void OnAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe) return;
            if (unit.IsMe)
            {
                if (target is Obj_AI_Hero && !ObjectManager.Player.IsWindingUp)
                {
                    if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Combo)
                    {
                        if (W.IsReady() && 
                            Config.Item("UseWCombo").GetValue<bool>() &&
                            Orbwalking.InAutoAttackRange(target)
                            )
                        {
                            W.Cast();                           
                            ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                        }
                        if (Config.Item("UseItemCombo").GetValue<bool>() && Orbwalking.InAutoAttackRange(target))
                        {
                            UseItems(2, null);
                        }
                    }
                    if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Harass)
                    {
                        if (W.IsReady() && 
                            Config.Item("UseWHarass").GetValue<bool>() &&
                            Orbwalking.InAutoAttackRange(target))
                        {
                            W.Cast();
                            ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                        }
                    }
                }
                if (target is Obj_AI_Minion && !ObjectManager.Player.IsWindingUp && W.IsReady() && !ObjectManager.Player.IsWindingUp && Orbwalking.InAutoAttackRange(target))
                {
                    if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.LaneClear && GetPlayerManaPercentage() > Config.Item("FarmMana").GetValue<Slider>().Value)
                    {
                        if (Config.Item("UseWFarm").GetValue<bool>())
                        {
                            W.Cast();
                            ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                        }
                    }
                    if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.JungleClear)
                    {                        
                        if (Config.Item("UseWJFarm").GetValue<bool>())
                        {
                            W.Cast();
                            ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                        }
                    }
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
            //var target = LockedTargetSelector.GetTarget(Q.Range * 1.5f, TargetSelector.DamageType.Magical);
            Obj_AI_Hero target;
            if (MYOrbwalker.ComboLocked)
            {
                target = LockedTargetSelector.GetTarget(Q.Range * 1.5f, TargetSelector.DamageType.Physical);
            }
            else
            {
                target = MYOrbwalker.GetEnemyChampion();
            }
            var UseQ = Config.Item("UseQCombo").GetValue<bool>();
            var UseE = Config.Item("UseECombo").GetValue<bool>();
            var UseR = Config.Item("UseRCombo").GetValue<bool>();
            var Sticks = Config.Item("Sticky").GetValue<bool>();            
            if (Sticks)
            {
                if (target.IsValidTarget()) SetOrbwalkingToTarget(target);
            }   
            if (target.IsValidTarget())
            {
                if (target.InFountain()) return;              
                try
                {
                    if (UseQ && Q.IsReady() && !ObjectManager.Player.IsWindingUp && Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) < Q.Range)
                    {
                        if (target.ServerPosition.UnderTurret(true) && !Config.Item("TurretDive").GetValue<bool>()) return;
                        //QPred(target);
                        Q.Cast(ObjectManager.Player.ServerPosition.Extend(target.ServerPosition, Q.Range));
                    }
                    if (UseE && E.IsReady() && HaveBuff(target) && !ObjectManager.Player.IsDashing())
                    {
                        E.Cast();
                    }   
                    if (UseR && R.IsReady())
                    {
                        RPred(target);
                    }             
                }
                catch { }
            }
            else SetOrbwalkToDefault();
        }
        private static void Harass()
        {
            var UseQ = Config.Item("UseQHarass").GetValue<bool>();
            var UseE = Config.Item("UseEHarass").GetValue<bool>();
            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            if (target.IsValidTarget())
            {
                if (UseQ && Q.IsReady() && !ObjectManager.Player.IsWindingUp && !Orbwalking.InAutoAttackRange(target) && !target.UnderTurret(true) && Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) < Q.Range)
                {
                    Q.Cast(ObjectManager.Player.ServerPosition.Extend(target.ServerPosition, Q.Range));
                }
                if (UseE && E.IsReady() && HaveBuff(target) && !ObjectManager.Player.IsWindingUp && !Orbwalking.InAutoAttackRange(target) && !ObjectManager.Player.IsDashing())
                {
                    E.Cast();
                }
            }
        }
        private static void LaneClear()
        {
            if (GetPlayerManaPercentage() < Config.Item("FarmMana").GetValue<Slider>().Value) return;
            if (Config.Item("UseQFarm").GetValue<bool>() && Q.IsReady())
            {
                var AllMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Enemy);
                switch (Config.Item("QFarmType").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        //Any
                        MinionManager.FarmLocation QLine = Q2.GetLineFarmLocation(AllMinionsQ);
                        if (QLine.Position.IsValid() && !QLine.Position.To3D().UnderTurret(true) && Vector3.Distance(ObjectManager.Player.ServerPosition, QLine.Position.To3D()) > ObjectManager.Player.AttackRange)
                        {
                            if (QLine.MinionsHit > Config.Item("QFarmValue").GetValue<Slider>().Value) Q2.Cast(QLine.Position);
                        }
                        break;
                    case 1:
                        //Furthest
                        var FurthestQ = AllMinionsQ.OrderByDescending(i => i.Distance(ObjectManager.Player)).FirstOrDefault(x => !x.UnderTurret(true));
                        if (FurthestQ != null && FurthestQ.Position.IsValid() && !Orbwalking.InAutoAttackRange(FurthestQ))
                        {
                            Q2.Cast(FurthestQ.Position);
                        }
                        break;
                }
            }
            if (Config.Item("UseEFarm").GetValue<bool>() && E.IsReady() && !ObjectManager.Player.IsDashing())
            {
                switch (Config.Item("EFarmType").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        //Any
                        var AnyMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).Count(x => MinionBuffCheck(x));
                        if (AnyMinionsE > 0)
                        {
                            E.Cast();
                        }
                        break;
                    case 1:
                        //Most
                        var HaveEBuff = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).Count(x => MinionBuffCheck(x) && E.IsKillable(x));
                        var NoEBuff = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).Count(x => !MinionBuffCheck(x));
                        if (HaveEBuff >= NoEBuff)
                        {
                            E.Cast();
                        }
                        break;
                    case 2:
                        //Siege
                        var siegeE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).Count(x => x.BaseSkinName.Contains("Siege") && MinionBuffCheck(x) && E.IsKillable(x));
                        if (siegeE > 0)
                        {
                            E.Cast();
                        }
                        break;
                }
            }
        }
        private static void JungleClear()
        {
            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q2.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            var largemobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q2.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.Health).FirstOrDefault(x => !x.BaseSkinName.Contains("Mini"));
            if (mobs.Count <= 0) return;
            var mob = mobs[0];
            if (mob != null)
            {               
                if (Config.Item("UseQJFarm").GetValue<bool>() && Q2.IsReady() && Q2.IsInRange(mob))
                {
                    if (largemobs != null)
                    {
                        Q2.Cast(ObjectManager.Player.ServerPosition.Extend(largemobs.ServerPosition, Q2.Range));                        
                    }
                    else
                    {
                        Q2.Cast(ObjectManager.Player.ServerPosition.Extend(mob.ServerPosition, Q2.Range));
                    }
                }
                if (Config.Item("UseEJFarm").GetValue<bool>() && E.IsReady() && !ObjectManager.Player.IsDashing())
                {
                    var mobsE = mobs.Count(x => x.HasBuff("sejuanifrost") && E.IsInRange(x));
                    if (largemobs != null && largemobs.HasBuff("sejuanifrost") && E.IsKillable(largemobs))
                    {
                        E.Cast();
                    }
                    else if (mobsE > 0)
                    {
                        E.Cast();
                    }
                }
            }
        }
        private static void UseItems(int type, Obj_AI_Base target)
        {
            Int16[] SelfItems = { 3142 }; //3180, 3131, 3074, 3077, 
            Int16[] TargetingItems = { 3153, 3144, 3188, 3128, 3146, 3184 };
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
      
        private static bool HaveBuff(Obj_AI_Base target)
        {
            return target.HasBuff("SejuaniFrost");
        }
        private static bool MinionBuffCheck(Obj_AI_Base minion)
        {
            var buffs = minion.Buffs;
            foreach (var buff in buffs)
            {
                if (buff.Name == "sejuanifrost")
                    return true;
            }
            return false;
        }
        private static HitChance QHitChance
        {
            get
            {
                return GetQHitChance();
            }
        }
        private static HitChance GetQHitChance()
        {
            switch (Config.Item("QPredHitchance").GetValue<StringList>().SelectedIndex)
            {
                case 0:
                    return HitChance.Low;
                case 1:
                    return HitChance.Medium;
                case 2:
                    return HitChance.High;
                default:
                    return HitChance.Medium;
            }
        }
        private static void QPred(Obj_AI_Hero target)
        {
            PredictionOutput pred = Q.GetPrediction(target);
            if (pred.Hitchance >= QHitChance && Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) < 650)
            {                
                Q.Cast(target);
            }
        }
        private static void RPred(Obj_AI_Hero target)
        {
            foreach (var targets in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsEnemy && !x.IsDead && Vector3.Distance(ObjectManager.Player.ServerPosition, x.ServerPosition) < R.Range).OrderByDescending(i => i.CountEnemiesInRange(350f)))
            {
                R.Cast(targets);
                break;
            }
        }
    }
}
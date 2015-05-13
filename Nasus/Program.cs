using System;
using System.Collections.Generic;
using System.Linq;
using myOrbwalker;
using LeagueSharp.Common.Data;
using LeagueSharp.Common;
using LeagueSharp;
using SharpDX;
using Color = System.Drawing.Color;


namespace Nasus
{
    class Program
    {
        private const string ChampionName = "Nasus";
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
            W = new Spell(SpellSlot.W, 600);
            E = new Spell(SpellSlot.E, 650);
            R = new Spell(SpellSlot.R, 0);
            E.SetSkillshot(E.Instance.SData.SpellCastTime, E.Instance.SData.LineWidth, E.Instance.SData.MissileSpeed, false, SkillshotType.SkillshotCircle);

            Config = new Menu("Nasus", "Nasus", true);
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
            Config.SubMenu("Combo").AddItem(new MenuItem("CRType", "R").SetValue(new StringList(new[] { "Always", "Low Hp" })));
            Config.SubMenu("Combo").AddItem(new MenuItem("CRHP", "HP <").SetValue(new Slider(50)));
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
            Config.SubMenu("Farm").AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseQLastHit", "(Last Hit Key) Use Q").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseEFarm", "Use E").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("EFarmValue", "E More Than").SetValue(new Slider(1, 1, 5)));
            Config.SubMenu("Farm").AddItem(new MenuItem("FarmMana", "Farm Mana >").SetValue(new Slider(50)));

            Config.AddSubMenu(new Menu("Jungle Farm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(true));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("CircleTarget", "Circle Target").SetValue(true));

            Config.AddToMainMenu();
            Game.OnUpdate += Game_OnUpdate;
            MYOrbwalker.AfterAttack += OnAfterAttack;
            MYOrbwalker.BeforeAttack += BeforeAttack;
            MYOrbwalker.OnNonKillableMinion += OnNonKillableMinion;
            Spellbook.OnCastSpell += OnCastSpell;
            Drawing.OnEndScene += Drawing_OnEndScene;
        }
        private static void OnNonKillableMinion(AttackableUnit minion)
        {
            if (Config.Item("UseQFarm").GetValue<bool>() && Q.IsReady() && GetPlayerManaPercentage() > Config.Item("FarmMana").GetValue<Slider>().Value)
            {
                var target = minion as Obj_AI_Base;
                if (target != null && Q.IsKillable(target) && Orbwalking.InAutoAttackRange(target))
                {
                    Q.Cast();
                }
            }
        }
        private static void OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (args.Target is Obj_AI_Hero && args.Target.IsEnemy)
            {
                if (sender.Owner.IsMe && args.Slot == SpellSlot.E)
                {
                    if ((MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Combo && Config.Item("UseQCombo").GetValue<bool>() ||
                        MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Harass && Config.Item("UseQHarass").GetValue<bool>()) &&
                        !QBuff() && 
                        Q.IsReady() && 
                        Q.IsKillable((Obj_AI_Hero)args.Target) && 
                        Orbwalking.InAutoAttackRange((Obj_AI_Hero)args.Target))
                    {
                        args.Process = false;
                    }
                }
            }
        }
        private static void BeforeAttack(MYOrbwalker.BeforeAttackEventArgs args)
        {
            if (args.Target is Obj_AI_Hero && args.Target.IsEnemy)
            {                
                if ((MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Combo && Config.Item("UseQCombo").GetValue<bool>() ||
                    MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Harass && Config.Item("UseQHarass").GetValue<bool>()) &&
                    !QBuff() && 
                    Q.IsReady() && 
                    Q.IsKillable(args.Target) &&
                    Orbwalking.InAutoAttackRange(args.Target))
                {
                    Q.Cast();
                }
            }
            
            if (args.Target is Obj_AI_Minion)
            {
                if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.LaneClear &&
                    !QBuff() &&
                    Config.Item("UseQFarm").GetValue<bool>() && Q.IsReady() &&
                    GetPlayerManaPercentage() > Config.Item("FarmMana").GetValue<Slider>().Value &&
                    MYOrbwalker.IsWaiting() &&
                    Orbwalking.InAutoAttackRange(args.Target))
                {
                    Q.Cast();
                }
            }
            if (args.Target is Obj_AI_Minion && args.Target.Team == GameObjectTeam.Neutral)
            {
                if (!QBuff() &&
                    !args.Target.Name.Contains("Mini") &&
                    Config.Item("UseQJFarm").GetValue<bool>() && Q.IsReady() &&  
                    Q.IsKillable(args.Target) &&
                    Orbwalking.InAutoAttackRange(args.Target))
                {
                    Q.Cast();
                }
            }
        }
        private static void OnAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe) return;
            if (unit.IsMe)
            {
                if (!ObjectManager.Player.IsWindingUp &&
                    (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Combo && 
                    Config.Item("UseQCombo").GetValue<bool>() && 
                    Q.IsReady()) &&
                    Orbwalking.InAutoAttackRange(target))
                {
                    Q.Cast();
                }
                if (!ObjectManager.Player.IsWindingUp && !QBuff() &&
                    (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Combo && 
                    Config.Item("UseItemCombo").GetValue<bool>() && 
                    Orbwalking.InAutoAttackRange(target)))
                {
                    UseItems(2, null);
                }
            }
        }
        private static void Drawing_OnEndScene(EventArgs args)
        {
            if (ObjectManager.Player.IsDead) return;
            if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Combo && Config.Item("CircleTarget").GetValue<bool>())
            {
                var target = MYOrbwalker.ComboLocked ? LockedTargetSelector._lastTarget : MYOrbwalker.GetEnemyChampion();
                if (target != null) Render.Circle.DrawCircle(target.Position, 125, Color.Lime, 7, true);
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
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active) Combo();
            if (Config.Item("HarassActive").GetValue<KeyBind>().Active) Harass();
            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active) LaneClear();                
            if (Config.Item("JungleActive").GetValue<KeyBind>().Active) JungleClear();
            if (Config.Item("LastHitActive").GetValue<KeyBind>().Active)
            {
                if (MYOrbwalker.IsWaiting() && !ObjectManager.Player.IsWindingUp && Config.Item("UseQLastHit").GetValue<bool>() && Q.IsReady())
                {
                    Q.Cast();
                }
            }
        }
        private static void Combo()
        {     
            Obj_AI_Hero target;
            if (MYOrbwalker.ComboLocked)
            {
                target = LockedTargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
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
            var CRType = Config.Item("CRType").GetValue<StringList>().SelectedIndex;
            if (Sticks)
            {
                if (target.IsValidTarget()) SetOrbwalkingToTarget(target);
            }
            if (target.IsValidTarget())
            {               
                //if (CastItems) { UseItems(0, target); }
                try
                {                    
                    if (UseW && W.IsReady() && W.IsInRange(target))
                    {
                        W.CastOnUnit(target);
                    }
                    if (UseE && E.IsReady() && !Q.IsKillable(target))
                    {
                        if (ObjectManager.Player.UnderTurret(true) && target.UnderTurret(true) && !Config.Item("TurretDive").GetValue<bool>()) return;
                        EPredict(target);
                    }
                    if (UseR && R.IsReady())
                    {
                        switch (CRType)
                        {
                            case 0:
                                R.Cast();
                                break;
                            case 1:
                                if (GetPlayerHealthPercentage() < Config.Item("CRHP").GetValue<Slider>().Value)
                                {
                                    R.Cast();
                                }
                                break;
                        }
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
            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            if (target.IsValidTarget())
            {
                if (UseW && W.IsReady() && W.IsInRange(target) && !Orbwalking.InAutoAttackRange(target))
                {
                    if (ObjectManager.Player.UnderTurret(true) && target.UnderTurret(true)) return;
                    W.CastOnUnit(target);
                }
                if (UseE && E.IsReady())
                {
                    if (ObjectManager.Player.UnderTurret(true) && target.UnderTurret(true)) return;
                    EPredict(target);
                }
            }
        }
        private static void LaneClear()
        {
            if (GetPlayerManaPercentage() < Config.Item("FarmMana").GetValue<Slider>().Value) return;          
            if (Config.Item("UseEFarm").GetValue<bool>() && E.IsReady() && !ObjectManager.Player.IsWindingUp)
            {    
                var epos = GetEPos();
                if (epos.Item1 == 0) return;
                if (epos.Item1 > Config.Item("EFarmValue").GetValue<Slider>().Value)
                {
                    if (epos.Item2.UnderTurret(true) && ObjectManager.Player.UnderTurret(true)) return;
                    E.Cast(epos.Item2.Shorten(ObjectManager.Player.ServerPosition, 50f));
                }
            }
        }
        private static void JungleClear()
        {
            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            if (mobs.Count <= 0) return;
            var UseQ = Config.Item("UseQJFarm").GetValue<bool>();
            var UseE = Config.Item("UseEJFarm").GetValue<bool>();
            var mob = mobs[0];
            if (mob != null)
            {
                if (UseQ && Q.IsReady() && Orbwalking.InAutoAttackRange(mob))
                {
                    Q.Cast();
                }
                if (UseE && E.IsReady())
                {
                    List<Obj_AI_Base> MobsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                    MinionManager.FarmLocation ECircular = E.GetCircularFarmLocation(MobsE);
                    if (ECircular.MinionsHit > 0)
                    {
                        E.Cast(ECircular.Position.To3D().Shorten(ObjectManager.Player.ServerPosition, 50f));
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
        private static bool QBuff()
        {
            return ObjectManager.Player.HasBuff("NasusQ");
        }
        private static void EPredict(Obj_AI_Base target)
        {
            var nearChamps = (from champ in ObjectManager.Get<Obj_AI_Hero>() where champ.IsValidTarget(E.Range) && target != champ select champ).ToList();
            if (nearChamps.Count > 0)
            {
                var closeToPrediction = new List<Obj_AI_Hero>();
                foreach (var enemy in nearChamps)
                {
                    PredictionOutput prediction = E.GetPrediction(enemy);
                    if (prediction.Hitchance >= HitChance.High && Vector3.Distance(ObjectManager.Player.ServerPosition, enemy.ServerPosition) < 400f)
                    {
                        closeToPrediction.Add(enemy);
                    }
                }
                if (closeToPrediction.Count == 0)
                {
                    PredictionOutput pred = E.GetPrediction(target);
                    if (pred.Hitchance >= HitChance.High && Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) < E.Range)
                    {
                        E.Cast(pred.CastPosition.Extend(ObjectManager.Player.ServerPosition, -100f)); //behind target
                    }
                }
                else if (closeToPrediction.Count > 0)
                {
                    E.CastIfWillHit(target, closeToPrediction.Count, false);
                }
            }
        }

        private static int GetEHits(Obj_AI_Base target)
        {
            var laneMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range);
            return laneMinions.Count(minion => Vector3.Distance(minion.ServerPosition, target.ServerPosition) < 400);
        }
        private static Tuple<int, Vector3> GetEPos()
        {
            Tuple<int, Vector3> bestSoFar = Tuple.Create(0, ObjectManager.Player.Position);
            var laneMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range);
            laneMinions.Reverse();
            foreach (var minion in laneMinions)
            {

                var hitCount = GetEHits(minion);
                if (hitCount > bestSoFar.Item1)
                {
                    bestSoFar = Tuple.Create(hitCount, minion.ServerPosition);
                }
            }
            return bestSoFar;
        }
        
    }
}
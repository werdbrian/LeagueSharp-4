using System;
using System.Collections.Generic;
using System.Linq;
using myOrbwalker;
using LeagueSharp.Common;
using LeagueSharp;
using SharpDX;
using Color = System.Drawing.Color;

namespace Diana
{
    class Program
    {
        private const string ChampionName = "Diana";        
        private static Spell Q, W, E, R;
        private static Obj_AI_Hero lastTarget;
        private static List<Obj_AI_Hero> AllEnemy { get; set; }
        private static Menu Config;
        private static Obj_SpellMissile QMissile;
        private static bool QCasted;
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName != ChampionName) return;
            Q = new Spell(SpellSlot.Q, 830f);
            W = new Spell(SpellSlot.W, 200f);
            E = new Spell(SpellSlot.E, 350f);
            R = new Spell(SpellSlot.R, 825f);

            Config = new Menu("Diana", "Diana", true);
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
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseR2Combo", "Use R (Second)").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("CType", "Combo Style").SetValue(new StringList(new[] { "Q-R-W-E", "R-Q-W-E" })));
            Config.SubMenu("Combo").AddItem(new MenuItem("TurretDive", "Turret Dive").SetValue(false));
            Config.SubMenu("Combo").AddItem(new MenuItem("Sticky", "Stick to Target").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("StickyType", "Type").SetValue(new StringList(new[] { "AA Range", "Slider" })));
            Config.SubMenu("Combo").AddItem(new MenuItem("StickyTypeSlider", "Range").SetValue(new Slider(50, 50, 1000)));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseItemCombo", "Use Items").SetValue(true));
            
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("QHarassValue", "(Try)").SetValue(new Slider(1, 1, 5)));
            
            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(false));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseWFarm", "Use W").SetValue(false));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseRFarm", "Use R").SetValue(false));
            Config.SubMenu("Farm").AddItem(new MenuItem("QFarmValue", "Q More Than").SetValue(new Slider(1, 1, 5)));
            Config.SubMenu("Farm").AddItem(new MenuItem("RFarmType", "R").SetValue(new StringList(new[] { "Any (Q buff)", "Siege", "Siege (Q buff)" })));
            Config.SubMenu("Farm").AddItem(new MenuItem("FarmMana", "Farm Mana >").SetValue(new Slider(50)));

            Config.AddSubMenu(new Menu("Jungle Farm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseWJFarm", "Use W").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseRJFarm", "Use R").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("RJFarmType", "R").SetValue(new StringList(new[] { "Any (Q buff)", "Large", "Large (Q buff)", "Secure" })));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("UseWShield", "Auto W").SetValue(false));

            Config.AddToMainMenu();            
            Game.OnUpdate += Game_OnUpdate;
            GameObject.OnCreate += OnCreate;
            GameObject.OnDelete += OnDelete;
            MYOrbwalker.AfterAttack += OnAfterAttack;
            Obj_AI_Base.OnProcessSpellCast += Game_OnProcessSpell;
            Drawing.OnEndScene += Drawing_OnEndScene;
        }
        private static void Drawing_OnEndScene(EventArgs args)
        {
            var target = MYOrbwalker.ComboLocked ? LockedTargetSelector._lastTarget : MYOrbwalker.GetEnemyChampion();  
            if (target != null) Render.Circle.DrawCircle(target.Position, 125, Color.Lime, 7, true);
        }

        private static void Game_OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs spell)
        {
            if (!unit.IsMe && spell.Target.IsMe && unit.IsChampion(unit.BaseSkinName))
            {
                if (MYOrbwalker.CurrentMode != MYOrbwalker.Mode.Combo || MYOrbwalker.CurrentMode == MYOrbwalker.Mode.None)
                {
                    if (Config.Item("UseWShield").GetValue<bool>())
                    {
                        Utility.DelayAction.Add(400, () => W.Cast());
                    }
                }      
            }
            if (unit.IsMe)
            {
                if (spell.SData.Name.ToLower() == "dianateleport" )
                {
                    if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Combo && Config.Item("UseWCombo").GetValue<bool>() && W.IsReady())
                    {
                        W.Cast();
                    }
                }
            }
        }
        private static void BeforeAttack(MYOrbwalker.BeforeAttackEventArgs args)
        {
            if (args.Target is Obj_AI_Minion && args.Target.Team == GameObjectTeam.Neutral)
            {
                if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.JungleClear &&
                    Config.Item("UseWJFarm").GetValue<bool>() && W.IsReady() &&
                    !args.Target.Name.Contains("Mini") &&
                    !ObjectManager.Player.IsWindingUp &&
                    Orbwalking.InAutoAttackRange(args.Target))
                {
                    UseItems(2, null);
                    W.Cast();
                }
            }
        }
        private static void OnAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe) return;
            if (unit.IsMe)
            {
                if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.JungleClear)
                {
                    if (target is Obj_AI_Minion && target.Team == GameObjectTeam.Neutral && !target.Name.Contains("Mini") &&
                       !ObjectManager.Player.IsWindingUp && Orbwalking.InAutoAttackRange(target))
                    {
                        UseItems(2, null);
                        if (Config.Item("UseWJFarm").GetValue<bool>() && W.IsReady())
                        {
                            W.Cast();
                        }
                        if (Config.Item("UseEJFarm").GetValue<bool>() && E.IsReady())
                        {
                            E.Cast();
                        }
                    }
                }
            }
        }
        private static void OnCreate(GameObject sender, EventArgs args)
        {
            var missle = (Obj_SpellMissile)sender;
            if (missle.SpellCaster.Name == ObjectManager.Player.Name && (missle.SData.Name == "dianaarcthrow"))
            {
                QMissile = missle;
                QCasted = true;
            }
        }
        private static void OnDelete(GameObject sender, EventArgs args)
        {
            var missle = (Obj_SpellMissile)sender;
            if (missle.SpellCaster.Name == ObjectManager.Player.Name && (missle.SData.Name == "dianaarcthrow"))
            {
                QMissile = null;
                QCasted = false;
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
            
        }
        private static void Combo()
        {
            Obj_AI_Hero target;
            if (MYOrbwalker.ComboLocked)
            {
                target = LockedTargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
            }
            else
            {
                target = MYOrbwalker.GetEnemyChampion();
            }
            var UseQ = Config.Item("UseQCombo").GetValue<bool>();
            var UseE = Config.Item("UseECombo").GetValue<bool>();
            var UseR = Config.Item("UseRCombo").GetValue<bool>();
            var UseR2 = Config.Item("UseR2Combo").GetValue<bool>();
            var CType = Config.Item("CType").GetValue<StringList>().SelectedIndex;
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
                    switch (CType)
                    {
                        case 0:
                            if (UseQ && Q.IsReady() && Q.IsInRange(target))
                            {
                                if (Q.GetPrediction(target).Hitchance >= HitChance.High)
                                {
                                    Q.CastIfHitchanceEquals(target, HitChance.High);
                                }
                            }
                            if (UseR && R.IsReady() && R.IsInRange(target) && (QCasted || target.HasBuff("dianamoonlight")))
                            {
                                if (target.UnderTurret(true) && !Config.Item("TurretDive").GetValue<bool>()) return;
                                if (target.UnderTurret(true) && Config.Item("TurretDive").GetValue<bool>() && GetPlayerHealthPercentage() < 33) return;
                                R.Cast(target);
                            }                
                            if (UseE && E.IsReady() && Orbwalking.InAutoAttackRange(target))
                            {
                                E.Cast();
                            }
                            if (UseR2 && R.IsReady() && !Q.IsReady() && R.IsInRange(target))
                            {
                                R.Cast(target);
                            }
                            if (Orbwalking.InAutoAttackRange(target))
                            {
                                if (Passive() || HaveSheen()) ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                                if (!ObjectManager.Player.IsWindingUp) UseItems(2, null);     
                            }
                            break;
                        case 1:
                            if (UseQ && UseR && Q.IsReady() && R.IsReady() && Q.IsInRange(target))
                            {
                                if (Q.GetPrediction(target).Hitchance >= HitChance.High)
                                {
                                    R.Cast(target);
                                    Q.CastIfHitchanceEquals(target, HitChance.High);
                                }
                            }                        
                            if (UseE && E.IsReady() && Orbwalking.InAutoAttackRange(target))
                            {
                                E.Cast();
                            }
                            if (UseR2 && R.IsReady() && !Q.IsReady() && R.IsInRange(target))
                            {
                                R.Cast(target);
                            }
                            if (Orbwalking.InAutoAttackRange(target))
                            {
                                if (Passive() || HaveSheen()) ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                                if (!ObjectManager.Player.IsWindingUp) UseItems(2, null);     
                            }
                            break;
                    }
                }
                catch { }                
            }
            else SetOrbwalkToDefault();
        }
        private static void Harass()
        {
            var UseQ = Config.Item("UseQHarass").GetValue<bool>();
            if (!ObjectManager.Player.UnderTurret(true))
            {
                if (UseQ && Q.IsReady())
                {
                    var tuple = GetQArcHero();
                    if (tuple.Item1 > Config.Item("QHarassValue").GetValue<Slider>().Value)
                    {
                        Q.Cast(tuple.Item2);
                    }
                }
            }
        }
        private static void LaneClear()
        {
            if (GetPlayerManaPercentage() < Config.Item("FarmMana").GetValue<Slider>().Value) return;
            var minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, ObjectManager.Player.AttackRange * 2, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.None);
            if (minions.Count >= 3 && MYOrbwalker.IsWaiting() && !ObjectManager.Player.IsWindingUp)
            {
                UseItems(2, null);
            }
            if (Config.Item("UseQFarm").GetValue<bool>() && Q.IsReady())
            {
                var tuple = GetQArc();
                if (tuple.Item1 > Config.Item("QFarmValue").GetValue<Slider>().Value)
                {
                    Q.Cast(tuple.Item2);
                }
            }
            if (Config.Item("UseWFarm").GetValue<bool>() && W.IsReady())
            {
                var allMinionsW = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, ObjectManager.Player.AttackRange, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health);
                if (allMinionsW.Count >= 3)
                {
                    W.Cast();
                }
            }
            if (Config.Item("UseRFarm").GetValue<bool>() && R.IsReady())
            {
                switch (Config.Item("RFarmType").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        var minionR = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, R.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).Where(x => !x.UnderTurret(true) && MinionBuffCheck(x) && R.IsKillable(x)).OrderByDescending(i => i.Distance(ObjectManager.Player)).FirstOrDefault();
                        R.CastOnUnit(minionR);
                        break;
                    case 1:
                        var siegeQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, R.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).FirstOrDefault(x => x.BaseSkinName.Contains("Siege") && !x.UnderTurret(true) && R.IsKillable(x));
                        R.CastOnUnit(siegeQ);
                        break;
                    case 2:
                        var buffedsiegeQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, R.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).FirstOrDefault(x => x.BaseSkinName.Contains("Siege") && !x.UnderTurret(true) && MinionBuffCheck(x) && R.IsKillable(x));
                        R.CastOnUnit(buffedsiegeQ);
                        break;
                }
            }
        }
        private static void JungleClear()
        {
            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            var largemobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, R.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.Health).FirstOrDefault(x => !x.BaseSkinName.Contains("Mini"));
            if (mobs.Count <= 0) return;
            var mob = mobs[0];
            if (mob == null) return;

            if (Config.Item("UseQJFarm").GetValue<bool>() && Q.IsReady())
            {
                if (largemobs != null)
                {
                    Q.Cast(largemobs.ServerPosition);
                }
                var mobQ = Q.GetCircularFarmLocation(mobs, Q.Width);
                if (mobQ.MinionsHit > 0)
                {
                    Q.Cast(mobQ.Position);
                }
            }
           
            if (Config.Item("UseRJFarm").GetValue<bool>() && R.IsReady())
            {
                switch (Config.Item("RJFarmType").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                       //Any w/ buff
                        var MobQBuff = mobs.FirstOrDefault(x => MinionBuffCheck(x));
                        if (MobQBuff.IsValidTarget())
                        {
                            R.CastOnUnit(MobQBuff);
                        }
                        break;
                    case 1:
                       //Large
                        if (largemobs != null)
                        {
                            R.CastOnUnit(largemobs);
                        }
                        break;
                    case 2:
                        //Large w/buff
                        if (largemobs != null)
                        {
                            if (MinionBuffCheck(largemobs))
                            {
                                R.CastOnUnit(largemobs);
                            }
                        }
                        break;
                    case 3:
                        //Secure
                        if (largemobs != null)
                        {
                            if (MinionBuffCheck(largemobs) && R.IsKillable(largemobs))
                            {
                                R.CastOnUnit(largemobs);
                            }
                            else if (!MinionBuffCheck(largemobs) && R.IsKillable(largemobs))
                            {
                                R.CastOnUnit(largemobs);
                            }
                        }
                        break;
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
        private static bool Passive()
        {
            return ObjectManager.Player.HasBuff("dianaarcready");
        }
        private static bool HaveSheen()
        {
            return ObjectManager.Player.HasBuff("Sheen") || ObjectManager.Player.HasBuff("Lichbane");
        }
        private static bool MinionBuffCheck(Obj_AI_Base minion)
        {
           // return minion.HasBuff("dianamoonlight");            
            var buffs = minion.Buffs;
            foreach (var buff in buffs)
            {
                if (buff.Name == "dianamoonlight")
                    return true;
            }
            return false;
        }  
        // Detuks'
        private static int GetQHits(Obj_AI_Base target)
        {
            var laneMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range);
            return laneMinions.Count(minion => Vector3.Distance(minion.ServerPosition, target.ServerPosition) <= 200);
        }
        private static Tuple<int, Vector3> GetQArc()
        {
            Tuple<int, Vector3> bestSoFar = Tuple.Create(0, ObjectManager.Player.Position);
            var laneMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range);
            laneMinions.Reverse();
            foreach (var minion in laneMinions)
            {                
                var hitCount = GetQHits(minion);
                if (hitCount > bestSoFar.Item1)
                {
                    bestSoFar = Tuple.Create(hitCount, minion.ServerPosition);
                }
            }
            return bestSoFar;
        }        
        //
        private static int GetQHitsHero(Obj_AI_Base target)
        {
            var GetenemysHit = ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy && !enemy.IsDead && enemy.IsValidTarget()).ToList();
            return GetenemysHit.Count(enemy => Vector3.Distance(enemy.ServerPosition, target.ServerPosition) <= 200);
        }
        private static Tuple<int, Vector3> GetQArcHero()
        {
            AllEnemy = ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy && !enemy.IsDead && enemy.IsValidTarget()).Reverse().ToList(); 
            Tuple<int, Vector3> bestSoFar = Tuple.Create(0, ObjectManager.Player.ServerPosition);
            foreach (var enemy in AllEnemy)
            {
                var hitCount = GetQHitsHero(enemy);
                if (hitCount > bestSoFar.Item1)
                {
                    bestSoFar = Tuple.Create(hitCount, enemy.ServerPosition);
                }
            }
            return bestSoFar;
        }
    }
}
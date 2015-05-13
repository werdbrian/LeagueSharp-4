using System;
using System.Linq;
using myOrbwalker;
using LeagueSharp.Common;
using LeagueSharp;
using SharpDX;
using Color = System.Drawing.Color;


namespace Nocturne
{
    class Program
    {
        private const string ChampionName = "Nocturne";
        private static Menu Config;
        private static Spell Q, W, E, R, R2;
        private static Obj_AI_Hero lastTarget;        
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName != ChampionName) return;
            Q = new Spell(SpellSlot.Q, 1200f);
            W = new Spell(SpellSlot.W, 0f);
            E = new Spell(SpellSlot.E, 425f);
            R = new Spell(SpellSlot.R); //first cast global
            R2 = new Spell(SpellSlot.R, 2000f); //second cast targetted, limited range

            Q.SetSkillshot(0.25f, 60f, 1600f, false, SkillshotType.SkillshotLine);
            E.SetTargetted(0.5f, 1700f);
            R2.SetTargetted(0.75f, 2500f);

            Config = new Menu("Nocturne", "Nocturne", true);
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
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("TurretDive", "Turret Dive").SetValue(false));
            Config.SubMenu("Combo").AddItem(new MenuItem("Sticky", "Stick to Target").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("StickyType", "Type").SetValue(new StringList(new[] { "AA Range", "Slider" })));
            Config.SubMenu("Combo").AddItem(new MenuItem("StickyTypeSlider", "Range").SetValue(new Slider(50, 50, 1000)));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseItemCombo", "Use Items").SetValue(true));
            
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            
            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("QFarmType", "Q").SetValue(new StringList(new[] { "Any", "Siege" })));
            Config.SubMenu("Farm").AddItem(new MenuItem("QFarmValue", "(Any) Q More Than").SetValue(new Slider(1, 1, 5)));
            Config.SubMenu("Farm").AddItem(new MenuItem("FarmMana", "Farm Mana >").SetValue(new Slider(50)));

            Config.AddSubMenu(new Menu("Jungle Farm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(true));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("UseRKey", "Paranoia (R) Key").SetValue(new KeyBind(Config.Item("CustomMode_Key").GetValue<KeyBind>().Key, KeyBindType.Press)));  //T
            Config.SubMenu("Misc").AddItem(new MenuItem("UseRType", "R").SetValue(new StringList(new[] { "Less Hit", "R Killable", "YOLO", "Furthest", "Lowest HP" })));
            Config.SubMenu("Misc").AddItem(new MenuItem("UseRType1", "Hit <").SetValue(new Slider(4, 1, 10)));
            Config.SubMenu("Misc").AddItem(new MenuItem("ParanoiaDraw", "Draw").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("UseWShield", "Auto W").SetValue(true));

            Config.AddToMainMenu();
            MYOrbwalker.AfterAttack += OnAfterAttack;
            Obj_AI_Base.OnProcessSpellCast += Game_OnProcessSpell;
            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnEndScene += Drawing_OnEndScene;
        }
        private static void Game_OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs spell)
        {
            if (!unit.IsMe && spell.Target.IsMe && unit.IsChampion(unit.BaseSkinName) && !spell.SData.IsAutoAttack())
            {
                if (Config.Item("UseWShield").GetValue<bool>())
                {
                    Utility.DelayAction.Add(50, () => W.Cast());
                }
            }
        }
        private static void Drawing_OnEndScene(EventArgs args)
        {
            if (ObjectManager.Player.IsDead) return;            
            var target = MYOrbwalker.ComboLocked ? LockedTargetSelector._lastTarget : MYOrbwalker.GetEnemyChampion();  
            if (target != null) Render.Circle.DrawCircle(target.Position, 125, Color.Lime, 7, true);

            if (Config.Item("ParanoiaDraw").GetValue<bool>() && R.IsReady())
            {
                var EnemyList = HeroManager.AllHeroes.Where(x => x.IsValidTarget() && x.IsEnemy && !x.IsDead && !x.IsZombie && !x.IsInvulnerable);
                var ValidTargets = EnemyList.Where(x => !x.InFountain() && x.IsVisible && !Orbwalking.InAutoAttackRange(x) && Vector3.Distance(ObjectManager.Player.ServerPosition, x.ServerPosition) < R2.Range);               
                if (MYOrbwalker.CurrentMode != MYOrbwalker.Mode.Combo)
                {
                    switch (Config.Item("UseRType").GetValue<StringList>().SelectedIndex)
                    {
                        case 0:
                            var Type1 = ValidTargets.Where(i => (i.Health / ObjectManager.Player.GetAutoAttackDamage(i)) <= Config.Item("UseRType1").GetValue<Slider>().Value);
                            foreach (var starget in Type1)
                            {
                                Utility.DrawCircle(starget.Position, R2.Range, Color.Lime, 1, 30, true);
                            }
                            break;
                        case 1:
                            var Type2 = ValidTargets.Where(x => R2.IsKillable(x));
                            foreach (var starget in Type2)
                            {
                                Utility.DrawCircle(starget.Position, R2.Range, Color.Lime, 1, 30, true);
                            }
                            break;
                        case 2:
                            var Type3 = ValidTargets.OrderBy(i => i.Health).FirstOrDefault(x => x.Health < ObjectManager.Player.Health);
                            if (Type3 != null)
                            {
                                Utility.DrawCircle(Type3.Position, R2.Range, Color.Lime, 1, 30, true);
                            }
                            break;
                        case 3:
                            var Type4 = ValidTargets.OrderByDescending(i => i.Distance(ObjectManager.Player)).FirstOrDefault();
                            if (Type4 != null)
                            {
                                Utility.DrawCircle(Type4.Position, R2.Range, Color.Lime, 1, 30, true);
                            }
                            break;
                        case 4:
                            var Type5 = ValidTargets.OrderBy(i => i.Health).FirstOrDefault();
                            if (Type5 != null)
                            {
                                Utility.DrawCircle(Type5.Position, R2.Range, Color.Lime, 1, 30, true);
                            }
                            break;
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
        private static void Game_OnUpdate(EventArgs args)
        {
            if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.None)
            {
                LockedTargetSelector.UnlockTarget();
                SetOrbwalkToDefault();
            }
            SetRRange();            
            if (ObjectManager.Player.IsDead) return;
            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active) LaneClear();
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active) Combo();
            if (Config.Item("HarassActive").GetValue<KeyBind>().Active) Harass(); 
            if (Config.Item("JungleActive").GetValue<KeyBind>().Active) JungleClear();
            if (Config.Item("UseRKey").GetValue<KeyBind>().Active) Paranoia();            
        }
        private static void LaneClear()
        {
            if (GetPlayerManaPercentage() < Config.Item("FarmMana").GetValue<Slider>().Value) return;
            if (Config.Item("UseQFarm").GetValue<bool>() && Q.IsReady() && !MYOrbwalker.IsWaiting() && !ObjectManager.Player.IsWindingUp && !ObjectManager.Player.UnderTurret(true) && !Passive())
            {
                var AllMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Enemy);                
                switch (Config.Item("QFarmType").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        MinionManager.FarmLocation QLine = Q.GetLineFarmLocation(AllMinionsQ);
                        if (QLine.Position.IsValid() && Vector3.Distance(ObjectManager.Player.ServerPosition, QLine.Position.To3D()) > ObjectManager.Player.AttackRange)
                        {
                            if (QLine.MinionsHit > Config.Item("QFarmValue").GetValue<Slider>().Value) Q.Cast(QLine.Position);
                        }
                        break;
                    case 1:
                        var siegeQ = AllMinionsQ.FirstOrDefault(x => x.BaseSkinName.Contains("Siege"));
                        if (siegeQ != null && Vector3.Distance(ObjectManager.Player.ServerPosition, siegeQ.ServerPosition) > ObjectManager.Player.AttackRange)
                        {
                            Q.Cast(siegeQ.ServerPosition);
                        }
                        break;
                }
            }
        }
        private static void JungleClear()
        { 
            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            var largemobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.Health).FirstOrDefault(x => !x.BaseSkinName.Contains("Mini"));
            if (mobs.Count <= 0) return;
            var mob = mobs[0];
            if (mob != null)
            {
                if (Config.Item("UseQJFarm").GetValue<bool>() && Q.IsReady() && Q.IsInRange(mob))
                {
                    if (largemobs != null)
                    {
                        Q.Cast(ObjectManager.Player.ServerPosition.Extend(largemobs.ServerPosition, Q.Range));
                    }
                    else
                    {
                        Q.Cast(ObjectManager.Player.ServerPosition.Extend(mob.ServerPosition, Q.Range));
                    }
                }
                if (Config.Item("UseEJFarm").GetValue<bool>() && E.IsReady())
                {
                    if (largemobs != null && Orbwalking.InAutoAttackRange(largemobs))
                    {
                        E.CastOnUnit(largemobs);
                    }
                }
            }
        }
        private static void Combo()
        {
            Obj_AI_Hero target;
            if (MYOrbwalker.ComboLocked)
            {
                target = LockedTargetSelector.GetTarget(E.Range * 1.5f, TargetSelector.DamageType.Physical);
            }
            else
            {
                target = MYOrbwalker.GetEnemyChampion();
            }
            var UseQ = Config.Item("UseQCombo").GetValue<bool>();
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
                    if (UseQ && Q.IsReady() && Q.IsInRange(target))
                    {
                        Q.CastIfHitchanceEquals(target, HitChance.High);
                    }
                    if (UseE && E.IsReady() && Orbwalking.InAutoAttackRange(target) && !ObjectManager.Player.IsWindingUp)
                    {
                        E.CastOnUnit(target);
                    }
                    if (Orbwalking.InAutoAttackRange(target) && CastItems)
                    {
                        UseItems(1, target);
                    }
                }
                catch { }
            }
            else SetOrbwalkToDefault();
        }
        private static void Paranoia()
        {
            if (R.IsReady())
            {
                var EnemyList = HeroManager.AllHeroes.Where(x => x.IsValidTarget() && x.IsEnemy && !x.IsDead && !x.IsZombie && !x.IsInvulnerable);
                var ValidTargets = EnemyList.Where(x => !x.InFountain() && !Orbwalking.InAutoAttackRange(x) && Vector3.Distance(ObjectManager.Player.ServerPosition, x.ServerPosition) < R2.Range);
                switch (Config.Item("UseRType").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        var targetR = ValidTargets.OrderBy(i => (i.Health / ObjectManager.Player.GetAutoAttackDamage(i))).FirstOrDefault();
                        if (targetR != null && targetR.IsValidTarget() && (targetR.Health / ObjectManager.Player.GetAutoAttackDamage(targetR) <= Config.Item("UseRType1").GetValue<Slider>().Value))
                        {
                            if (targetR.UnderTurret(true) && !Config.Item("TurretDive").GetValue<bool>()) return;
                            R.Cast();
                            R2.CastOnUnit(targetR);
                        }
                        break;
                    case 1:
                        var targetR2 = ValidTargets.FirstOrDefault(x => R.IsKillable(x));
                        if (targetR2 != null && targetR2.IsValidTarget())
                        {
                            if (targetR2.UnderTurret(true) && !Config.Item("TurretDive").GetValue<bool>()) return;
                            R.Cast();
                            R2.CastOnUnit(targetR2);
                        }
                        break;
                    case 2:
                        var targetR3 = ValidTargets.OrderBy(i => i.Health).FirstOrDefault(x => x.Health < ObjectManager.Player.Health);
                        if (targetR3 != null && targetR3.IsValidTarget())
                        {
                            if (targetR3.UnderTurret(true) && !Config.Item("TurretDive").GetValue<bool>()) return;
                            R.Cast();
                            R2.CastOnUnit(targetR3);
                        }
                        break;
                    case 3:
                        var targetR4 = ValidTargets.OrderByDescending(i => i.Distance(ObjectManager.Player)).FirstOrDefault();
                        if (targetR4 != null && targetR4.IsValidTarget())
                        {
                            if (targetR4.UnderTurret(true) && !Config.Item("TurretDive").GetValue<bool>()) return;
                            R.Cast();
                            R2.CastOnUnit(targetR4);
                        }
                        break;
                    case 4:
                        var targetR5 = ValidTargets.OrderBy(i => i.Health).FirstOrDefault();
                        if (targetR5 != null && targetR5.IsValidTarget())
                        {
                            if (targetR5.UnderTurret(true) && !Config.Item("TurretDive").GetValue<bool>()) return;
                            R.Cast();
                            R2.CastOnUnit(targetR5);
                        }
                        break;
                }
            } 
        }
        private static void Harass()
        {
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (target != null)
            {         
                if (Config.Item("UseQHarass").GetValue<bool>() && Q.IsReady() && Q.IsInRange(target) && !ObjectManager.Player.UnderTurret(true))
                {
                    Q.CastIfHitchanceEquals(target, HitChance.High);
                }
            }
        }
        private static bool Passive()
        {
            return ObjectManager.Player.HasBuff("nocturneumbrablades");
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
        private static void SetRRange()
        {
            if (ObjectManager.Player.Spellbook.CanUseSpell(SpellSlot.R) == SpellState.NotLearned) return;
            R2.Range = 1250 + (750 * R.Level);
        }
        private static void UseItems(int type, Obj_AI_Base target)
        {
            Int16[] SelfItems = { 3142 }; //3180, 3131, 3074, 3077, 
            Int16[] TargetingItems = { 3153, 3144, 3188, 3128, 3146, 3184 };
            //Int16[] AoeItems = { 3074, 3077 }; //3180, 3131, 3074, 3077,             
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
    }
}
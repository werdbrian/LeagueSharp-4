using System;
using System.Linq;
using myOrbwalker;
using LeagueSharp.Common;
using LeagueSharp;
using SharpDX;
using Color = System.Drawing.Color;

namespace Pantheon
{
    class Program
    {
        private const string ChampionName = "Pantheon";
        private static Menu Config;
        private static Spell Q, W, E, R;
        private static Obj_AI_Hero lastTarget;    
        private static bool IsChanneling;
        private static Obj_SpellMissile QMissile;
        private static bool QCasted;
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName != ChampionName) return;

            Q = new Spell(SpellSlot.Q, 600);
            W = new Spell(SpellSlot.W, 600);
            E = new Spell(SpellSlot.E, 700);

            Q.SetTargetted(0.2f, 1700f);
            W.SetTargetted(0.2f, 1700f);
            E.SetSkillshot(0.25f, 15f * 2 * (float)Math.PI / 180, 2000f, false, SkillshotType.SkillshotCone);

            Config = new Menu("Pantheon", "Pantheon", true);
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
            Config.SubMenu("Keys").AddItem(new MenuItem("JungleClearActive", "Jungle Clear").SetValue(new KeyBind(Config.Item("JungleClear_Key").GetValue<KeyBind>().Key, KeyBindType.Press))); //v
            Config.SubMenu("Keys").AddItem(new MenuItem("FleeActive", "Flee").SetValue(new KeyBind(Config.Item("Flee_Key").GetValue<KeyBind>().Key, KeyBindType.Press)));  //space
            
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("TurretDive", "Turret Dive").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("HaveShield", "Shield Check").SetValue(false));
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
            Config.SubMenu("Farm").AddItem(new MenuItem("UseWFarm", "Use W").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseEFarm", "Use E").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("QFarmType", "Q").SetValue(new StringList(new[] { "Any", "Only Siege", "Furthest" })));
            Config.SubMenu("Farm").AddItem(new MenuItem("WFarmType", "W").SetValue(new StringList(new[] { "Any", "Only Siege", "Smart Shield" })));
            Config.SubMenu("Farm").AddItem(new MenuItem("EFarmType", "E").SetValue(new StringList(new[] { "Any", "Only Siege", "Most" })));
            Config.SubMenu("Farm").AddItem(new MenuItem("FarmMana", "Farm Mana >").SetValue(new Slider(50)));

            Config.AddSubMenu(new Menu("Jungle Farm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseWJFarm", "Use W").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(true));

            Config.AddSubMenu(new Menu("Misc Manager", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("UseWGap", "W Gapcloser").SetValue(false));

            Config.AddToMainMenu();

            Obj_AI_Base.OnProcessSpellCast += Game_OnProcessSpell;
            Game.OnUpdate += Game_OnUpdate;
            AntiGapcloser.OnEnemyGapcloser -= OnEnemyGapcloser;
            MYOrbwalker.AfterAttack += OnAfterAttack;
            Drawing.OnEndScene += Drawing_OnEndScene;
            GameObject.OnCreate += OnCreate;
            GameObject.OnDelete += OnDelete;
        }
        private static void OnCreate(GameObject sender, EventArgs args)
        {
            var missle = (Obj_SpellMissile)sender;
            if (missle.SpellCaster.Name == ObjectManager.Player.Name && missle.SData.Name == "PantheonQ")
            {
                //Game.PrintChat("Create " + missle.SData.Name);
                QMissile = missle;
                QCasted = true;
            }
        }
        private static void OnDelete(GameObject sender, EventArgs args)
        {
            var missle = (Obj_SpellMissile)sender;
            if (missle.SpellCaster.Name == ObjectManager.Player.Name && missle.SData.Name == "PantheonQ")
            {
                //Game.PrintChat("Delete" + missle.SData.Name);
                QMissile = null;
                QCasted = false;
            }
        }
        private static void Drawing_OnEndScene(EventArgs args)
        {
            if (ObjectManager.Player.IsDead) return;
            var target = MYOrbwalker.ComboLocked ? LockedTargetSelector._lastTarget : MYOrbwalker.GetEnemyChampion();
            if (target != null) Render.Circle.DrawCircle(target.Position, 125, Color.Lime, 7, true);

        }
        private static void OnAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe) return;
            if (unit.IsMe)
            {
                if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Combo)
                {                    
                    if (!ObjectManager.Player.IsWindingUp && Config.Item("UseItemCombo").GetValue<bool>() && Orbwalking.InAutoAttackRange(target))
                    {
                        UseItems(2, null);
                    }
                }
                if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.JungleClear)
                {
                    if (target is Obj_AI_Minion && target.Team == GameObjectTeam.Neutral && !target.Name.Contains("Mini") &&
                       !ObjectManager.Player.IsWindingUp && Orbwalking.InAutoAttackRange(target))
                    {
                        UseItems(2, null);                        
                    }
                }
            }
        }
        private static void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Config.Item("UseWGap").GetValue<bool>())
            {
                if (Orbwalking.InAutoAttackRange(gapcloser.Sender) && W.IsReady())
                {
                    W.CastOnUnit(gapcloser.Sender);
                }
            }
        }
        private static void Game_OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs spell)
        {
            if (unit.IsMe)
            {
                if (spell.SData.Name.ToLower() == "pantheonq")
                {
                    //MYOrbwalker.ResetAutoAttackTimer();
                }
                if (spell.SData.Name.ToLower() == "pantheonw")
                {
                    //MYOrbwalker.ResetAutoAttackTimer();
                }
                if (spell.SData.Name.ToLower() == "pantheone")
                {
                    IsChanneling = true;
                    Utility.DelayAction.Add(750, () => IsChanneling = false);
                }
                if (spell.SData.Name.ToLower() == "pantheonrjump")
                {
                    IsChanneling = true;
                }
                if (spell.SData.Name.ToLower() == "pantheonrfall")
                {
                    IsChanneling = false;
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
            MYOrbwalker.SetAttack(!Channeling());
            MYOrbwalker.SetMovement(!Channeling());
            if (Channeling()) return;
            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active) LaneClear();          
            if (Config.Item("JungleClearActive").GetValue<KeyBind>().Active) JungleClear();
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active) Combo();
            if (Config.Item("HarassActive").GetValue<KeyBind>().Active) Harass();
            
        }
        private static void LaneClear()
        {
            if (GetPlayerManaPercentage() < Config.Item("FarmMana").GetValue<Slider>().Value) return;
            var minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, ObjectManager.Player.AttackRange * 2, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.None);
            if (minions.Count >= 3 && !MYOrbwalker.IsWaiting() && !ObjectManager.Player.IsWindingUp)
            {
                UseItems(2, null);
            }               
            if (Config.Item("UseQFarm").GetValue<bool>() && Q.IsReady() && !ObjectManager.Player.IsWindingUp && !ObjectManager.Player.IsDashing())
            {
                var minionQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).ToList();
                switch (Config.Item("QFarmType").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        var SelectQ = minionQ.FirstOrDefault(x => Q.IsKillable(x) && !Orbwalking.InAutoAttackRange(x));
                        Q.CastOnUnit(SelectQ);
                        break;
                    case 1:
                        var siegeQ = minionQ.FirstOrDefault(x => x.BaseSkinName.Contains("Siege") && Q.IsKillable(x));
                        Q.CastOnUnit(siegeQ);
                        break;
                    case 2:
                        var FurthestQ = minionQ.OrderByDescending(i => i.Distance(ObjectManager.Player)).FirstOrDefault(x => Q.IsKillable(x) && !Orbwalking.InAutoAttackRange(x));
                        Q.CastOnUnit(FurthestQ);
                        break;
                }                
            }
            if (Config.Item("UseWFarm").GetValue<bool>() && W.IsReady() && !ObjectManager.Player.IsWindingUp && !QCasted)
            {
                var minionW = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).OrderBy(i => i.Distance(ObjectManager.Player)).Where(x => !x.UnderTurret(true)).ToList(); 
                switch (Config.Item("WFarmType").GetValue<StringList>().SelectedIndex)
                {
                    case 0:                 
                        var SelectW = minionW.FirstOrDefault(x => W.IsKillable(x));
                        W.CastOnUnit(SelectW);
                        break;
                    case 1:
                        var siegeW = minionW.FirstOrDefault(x => x.BaseSkinName.Contains("Siege") && W.IsKillable(x));
                        W.CastOnUnit(siegeW);
                        break;
                    case 2:
                        if (PassiveCount() <= 3 && !HaveShield())
                        {
                            W.CastOnUnit(minionW[0]);
                        }
                        break;
                }                
            }
            if (Config.Item("UseEFarm").GetValue<bool>() && E.IsReady() && !ObjectManager.Player.IsWindingUp && !ObjectManager.Player.UnderTurret(true))
            {
                switch (Config.Item("EFarmType").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        //Any
                        var AnyMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).ToList();
                        if (AnyMinionsE[0] != null)
                        {
                            E.Cast(AnyMinionsE[0].ServerPosition);
                        }
                        break;
                    case 1:
                        //Siege
                        var siegeE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).Where(x => x.BaseSkinName.Contains("Siege") && E.IsKillable(x)).ToList();
                        if (siegeE[0] != null)
                        {
                            E.Cast(siegeE[0].ServerPosition);
                        }
                        break;
                    case 2:
                        //Most
                        var rangedMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range + E.Width + 30, MinionTypes.Ranged);
                        var locE = W.GetCircularFarmLocation(rangedMinionsE, W.Width * 0.75f);
                        if (locE.MinionsHit >= 3 && E.IsInRange(locE.Position.To3D()))
                        {
                            E.Cast(locE.Position);
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
            if (mob != null && !ObjectManager.Player.IsWindingUp)
            {
                if (Config.Item("UseQJFarm").GetValue<bool>() && Q.IsReady() && Q.IsInRange(mob) && !ObjectManager.Player.IsDashing())
                {
                    if (largemobs != null)
                    {
                        Q.CastOnUnit(largemobs);
                    }
                    else
                    {
                        Q.CastOnUnit(mob);
                    }
                }
                if (Config.Item("UseWJFarm").GetValue<bool>() && W.IsReady() && W.IsInRange(mob) && !QCasted)
                {
                    if (PassiveCount() <= 3 && !HaveShield())
                    {
                        if (largemobs != null)
                        {
                            W.CastOnUnit(largemobs);
                        }
                        else
                        {
                            W.CastOnUnit(mob);
                        }
                    }
                }
                if (Config.Item("UseEJFarm").GetValue<bool>() && E.IsReady())
                {
                    if (largemobs != null)
                    {
                        E.Cast(largemobs); 
                    }
                    else
                    {
                        E.Cast(mob);
                    } 
                }
            }
        }
        private static void Combo()
        {
            //var target = LockedTargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical, false);
            Obj_AI_Hero target;
            if (MYOrbwalker.ComboLocked)
            {
                target = LockedTargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
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
                    if (UseQ && Q.IsReady() && Q.IsInRange(target) && !ObjectManager.Player.IsDashing())
                    {
                        Q.CastOnUnit(target);
                    }
                    if (UseW && W.IsReady() && W.IsInRange(target) && !QCasted)
                    {
                        if (target.UnderTurret(true) && !Config.Item("TurretDive").GetValue<bool>()) return;
                        if (target.UnderTurret(true) && Config.Item("TurretDive").GetValue<bool>() && Config.Item("HaveShield").GetValue<bool>() && !HaveShield()) return;
                        W.CastOnUnit(target);
                    }
                    if (UseE && E.IsReady())
                    {
                        if (target.HasBuffOfType(BuffType.Stun))
                            E.Cast(target);
                    }
                }
                catch { }
            }
            else SetOrbwalkToDefault();
        }
        private static void Harass()
        {
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical, false);
            var UseQ = Config.Item("UseQHarass").GetValue<bool>();
            var UseW = Config.Item("UseWHarass").GetValue<bool>();
            if (target.IsValidTarget())
            {
                if (UseQ && Q.IsReady() && Q.IsInRange(target) && !ObjectManager.Player.IsDashing())
                {
                    Q.CastOnUnit(target);
                }
                if (UseW && W.IsReady() && W.IsInRange(target) && !QCasted)
                {
                    W.CastOnUnit(target);
                }
            }
        }
        private static float GetComboDamage(Obj_AI_Base target)
        {
            var damage = 0d;
            if (Config.Item("UseQCombo").GetValue<bool>() && Q.IsReady())
                damage += (target.Health <= target.MaxHealth * 0.15) ? (ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q) * 2) : ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q);
            if (Config.Item("UseWCombo").GetValue<bool>() && W.IsReady())
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.W);
            if (Config.Item("UseECombo").GetValue<bool>() && E.IsReady())
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.E);
            return (float)damage;
        }
        private static bool Channeling()
        {
            return IsChanneling;
        }
        private static bool HaveShield()
        {
            return ObjectManager.Player.HasBuff("pantheonpassiveshield");
        }
        private static int PassiveCount()
        {
            foreach (var buffs in ObjectManager.Player.Buffs.Where(buffs => buffs.Name == "pantheonpassivecounter"))
            {
                return buffs.Count;
            }
            return 0;
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

﻿using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace myOrbwalker
{
    public static class MYOrbwalker
    {

        private static readonly string[] AttackResets =
        {
             "blindingdart", "khazixq", "khazixqlong", "sonaq",
            "pantheonew", "pantheoneq", "dariusnoxiantacticsonh", "fioraflurry", "garenq",
            "hecarimrapidslash", "jaxempowertwo", "jaycehypercharge", "leonashieldofdaybreak", "luciane", "lucianq",
            "monkeykingdoubleattack", "mordekaisermaceofspades", "nasusq", "nautiluspiercinggaze", "netherblade",
            "parley", "poppydevastatingblow", "powerfist", "renektonpreexecute", "rengarq", "shyvanadoubleattack",
            "sivirw", "takedown", "talonnoxiandiplomacy", "trundletrollsmash", "vaynetumble", "vie", "volibearq",
            "xenzhaocombotarget", "yorickspectral"
        };

        private static readonly string[] NoAttacks =
        {
            "jarvanivcataclysmattack", "monkeykingdoubleattack",
            "shyvanadoubleattack", "shyvanadoubleattackdragon", "zyragraspingplantattack", "zyragraspingplantattack2",
            "zyragraspingplantattackfire", "zyragraspingplantattack2fire"
        };

        private static readonly string[] Attacks =
        {
            "caitlynheadshotmissile", "frostarrow", "garenslash2",
            "kennenmegaproc", "lucianpassiveattack", "masteryidoublestrike", "quinnwenhanced", "renektonexecute",
            "renektonsuperexecute", "rengarnewpassivebuffdash", "trundleq", "xenzhaothrust", 
            "xenzhaothrust2", "xenzhaothrust3"
        };
        private static readonly string[] NoResets = { "Thresh", "Kalista" };

        private static Menu Menu;
        private static Obj_AI_Hero Player = ObjectManager.Player;
        private static Obj_AI_Hero ForcedTarget;
        private static Vector3 _custompoint;
        private static int _duration;
        private static Obj_AI_Minion _prevMinion;
        private static IEnumerable<Obj_AI_Hero> AllEnemys = ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy);
        private static IEnumerable<Obj_AI_Hero> AllAllys = ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsAlly);

        public delegate void BeforeAttackEvenH(BeforeAttackEventArgs args);
        public delegate void OnTargetChangeH(Obj_AI_Base oldTarget, Obj_AI_Base newTarget);
        public delegate void AfterAttackEvenH(Obj_AI_Base unit, Obj_AI_Base target);
        public delegate void OnAttackEvenH(Obj_AI_Base unit, Obj_AI_Base target);
        public delegate void OnNonKillableMinionH(AttackableUnit minion);

        public static event BeforeAttackEvenH BeforeAttack;
        public static event OnTargetChangeH OnTargetChange;
        public static event AfterAttackEvenH AfterAttack;
        public static event OnAttackEvenH OnAttack;
        public static event OnNonKillableMinionH OnNonKillableMinion;
        public enum Mode
        {
            Combo,
            Harass,
            LaneClear,
            JungleClear,
            LaneFreeze,
            Lasthit,
            Flee,
            Custom,
            None,
        }

        private static bool _drawing = true;        
        private static bool _attack = true;
        private static bool _movement = true;
        private static bool _disableNextAttack;
        private const float LaneClearWaitTimeMod = 2f;
        private static int _lastAATick;
        private static Obj_AI_Base _lastTarget;
        private static Spell _movementPrediction;
        private static int _lastMovement;
        private static int TickCount
        {
            get { return (int) (Game.ClockTime * 1000); }
        }
        private static bool CustomMode
        {
            get { return Menu.Item("CustomMode_Bool").GetValue<bool>();}
        }
        private static bool JungleMove
        {
            get { return Menu.Item("Melee_JungleMoveInAA").GetValue<bool>(); }
        }
        public static void AddToMenu(Menu menu)
        {
            _movementPrediction = new Spell(SpellSlot.Unknown, GetAutoAttackRange());
            _movementPrediction.SetTargetted(Player.BasicAttack.SpellCastTime, Player.BasicAttack.MissileSpeed);

            Menu = menu;
            var menuModes = new Menu("Modes", "Modes");
            {
                var modeCombo = new Menu("Combo", "Modes_Combo");
                modeCombo.AddItem(
                    new MenuItem("Combo_Key", "Key").SetValue(new KeyBind("X".ToCharArray()[0], KeyBindType.Press)));
                modeCombo.AddItem(new MenuItem("Combo_move", "Movement").SetValue(true));
                modeCombo.AddItem(new MenuItem("Combo_attack", "Attack").SetValue(true));
                
                menuModes.AddSubMenu(modeCombo);

                var modeHarass = new Menu("Harass", "Modes_Harass");
                modeHarass.AddItem(
                    new MenuItem("Harass_Key", "Key").SetValue(new KeyBind("S".ToCharArray()[0], KeyBindType.Press)));
                modeHarass.AddItem(new MenuItem("Harass_move", "Movement").SetValue(true));
                modeHarass.AddItem(new MenuItem("Harass_attack", "Auto Attack").SetValue(true));
                modeHarass.AddItem(new MenuItem("Harass_Lasthit", "Last Hit Minions").SetValue(true));
                menuModes.AddSubMenu(modeHarass);

                var modeLaneClear = new Menu("Lane Clear", "Modes_LaneClear");
                modeLaneClear.AddItem(
                    new MenuItem("LaneClear_Key", "Key").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
                modeLaneClear.AddItem(new MenuItem("LaneClear_move", "Movement").SetValue(true));
                modeLaneClear.AddItem(new MenuItem("LaneClear_attack", "Auto Attack").SetValue(true));
                modeLaneClear.AddItem(new MenuItem("LaneClear_pokes", "Auto Attack Enemy Champion").SetValue(false));
                menuModes.AddSubMenu(modeLaneClear);

                var modeLaneFreeze = new Menu("Lane Freeze", "Modes_LaneFreeze");
                modeLaneFreeze.AddItem(
                    new MenuItem("LaneFreeze_Key", "Key").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));
                modeLaneFreeze.AddItem(new MenuItem("LaneFreeze_move", "Movement").SetValue(true));
                modeLaneFreeze.AddItem(new MenuItem("LaneFreeze_attack", "Attack").SetValue(true));
                modeLaneFreeze.AddItem(new MenuItem("LaneFreeze_pokes", "Auto Attack Enemy Champion").SetValue(false));
                modeLaneFreeze.AddItem(new MenuItem("LaneFreeze_Lasthit", "Last Hit Minions").SetValue(true));
                menuModes.AddSubMenu(modeLaneFreeze);

                var modeLasthit = new Menu("Last Hit", "Modes_LastHit");
                modeLasthit.AddItem(
                    new MenuItem("LastHit_Key", "Key").SetValue(new KeyBind("A".ToCharArray()[0], KeyBindType.Press)));
                modeLasthit.AddItem(new MenuItem("LastHit_move", "Movement").SetValue(true));
                modeLasthit.AddItem(new MenuItem("LastHit_attack", "Auto Attack").SetValue(true));
                menuModes.AddSubMenu(modeLasthit);

                var modeJungleClear = new Menu("Jungle Clear", "Modes_JungleClear");
                modeJungleClear.AddItem(
                    new MenuItem("JungleClear_Key", "Key").SetValue(
                        new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
                modeJungleClear.AddItem(new MenuItem("JungleClear_move", "Movement").SetValue(true));
                modeJungleClear.AddItem(new MenuItem("JungleClear_attack", "Attack").SetValue(true));
                menuModes.AddSubMenu(modeJungleClear);

                var modeCustomMode = new Menu("Custom Key", "Modes_Custom");
                modeCustomMode.AddItem(new MenuItem("CustomMode_Bool", "Enabled").SetValue(true));
                modeCustomMode.AddItem(new MenuItem("CustomMode_Key", "Key").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
                modeCustomMode.AddItem(new MenuItem("CustomMode_move", "Movement").SetValue(true));
                modeCustomMode.AddItem(new MenuItem("CustomMode_attack", "Attack").SetValue(true));
                
                menuModes.AddSubMenu(modeCustomMode);

                var modeFlee = new Menu("Flee", "Modes_Flee");
                modeFlee.AddItem(new MenuItem("Flee_Key", "Key").SetValue(new KeyBind(32, KeyBindType.Press)));
                menuModes.AddSubMenu(modeFlee);
            }
            menu.AddSubMenu(menuModes);

            var menuBestTarget = new Menu("Target", "Target");
            menuBestTarget.AddItem(new MenuItem("ImmuneCheck", "Check Physical Immunity").SetValue(false));

            menu.AddSubMenu(menuBestTarget);

            var menuMelee = new Menu("Melee", "Melee");
            menuMelee.AddItem(new MenuItem("Melee_Prediction_Minion", "Movement Prediction (Minion)").SetValue(true));
            menuMelee.AddItem(new MenuItem("Melee_JungleMoveInAA", "Jungle Movement while Attacking").SetValue(false));
            menu.AddSubMenu(menuMelee);

            var menuMisc = new Menu("Misc", "Misc");            
            menuMisc.AddItem(
                new MenuItem("Misc_ExtraWindUp", "Extra Winduptime").SetValue(new Slider(0, 0, 400)));
            menuMisc.AddItem(new MenuItem("Misc_AutoWindUp", "Autoset Windup").SetValue(false));
            
            menuMisc.AddItem(
                new MenuItem("Misc_Humanizer", "Movement Delay").SetValue(new Slider(0, 0, 400)));
            menuMisc.AddItem(new MenuItem("Misc_Farmdelay", "Farm Delay").SetValue(new Slider(0, 0, 400)));
            menuMisc.AddItem(
                new MenuItem("Misc_AAReset", "AA Reset Delay").SetValue(new Slider(0, 0, 400)));

            menuMisc.AddItem(
                new MenuItem("Misc_HarassUnderTurret", "Harass Under Turret").SetValue(false));
            menuMisc.AddItem(
                new MenuItem("Misc_AllMovementDisabled", "Disable All Movement").SetValue(false));
            menuMisc.AddItem(new MenuItem("Misc_AllAttackDisabled", "Disable All Attacks").SetValue(false));
            menu.AddSubMenu(menuMisc);

            var menuDrawing = new Menu("Drawing", "Draw");
            menuDrawing.AddItem(
                new MenuItem("Draw_Lasthit", "Minion Lasthit").SetValue(new Circle(true, Color.Red)));
            menuDrawing.AddItem(
                new MenuItem("Draw_nearKill", "Minion nearKill").SetValue(new Circle(true, Color.Yellow)));
            menu.AddSubMenu(menuDrawing);

            Drawing.OnDraw += OnDraw;
            Game.OnUpdate += OnUpdate;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
            //GameObject.OnCreate += Obj_SpellMissile_OnCreate;
            Spellbook.OnStopCast += SpellbookOnStopCast;
        }
        private static void Obj_SpellMissile_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.IsMe)
            {
                var obj = (Obj_AI_Hero)sender;
                if (obj.IsMelee())
                    return;
            }
            if (!(sender is Obj_SpellMissile) || !sender.IsValid)
                return;
            var missile = (Obj_SpellMissile)sender;
            if (missile.SpellCaster.IsValid<Obj_AI_Hero>() && IsAutoAttack(missile.SData.Name))
                FireAfterAttack(missile.SpellCaster, _lastTarget);
        }
        private static void SpellbookOnStopCast(Spellbook spellbook, SpellbookStopCastEventArgs args)
        {
            if (spellbook.Owner.IsValid && spellbook.Owner.IsMe && args.DestroyMissile && args.StopAnimation)
            {
                ResetAutoAttackTimer();
            }
        }
        private static void OnUpdate(EventArgs args)
        {           
                CheckAutoWindUp();
                if (CurrentMode == Mode.None) return;
                if (Player.IsCastingInterruptableSpell(true))  return;
                if (MenuGUI.IsChatOpen) return;
                var target = GetTarget();
                Orbwalk((_custompoint.To2D().IsValid()) ? _custompoint : Game.CursorPos, target);
        }
        private static void OnDraw(EventArgs args)
        {
            if (!_drawing)
                return;

            if (Menu.Item("Draw_Lasthit").GetValue<Circle>().Active ||
                Menu.Item("Draw_nearKill").GetValue<Circle>().Active)
            {
                var minionList = MinionManager.GetMinions(
                    Player.Position, GetAutoAttackRange() + 500, MinionTypes.All, MinionTeam.Enemy,
                    MinionOrderTypes.MaxHealth);
                foreach (var minion in minionList.Where(minion => minion.IsValidTarget(GetAutoAttackRange() + 500)))
                {

                    if (Menu.Item("Draw_Lasthit").GetValue<Circle>().Active &&
                        minion.Health <= Player.GetAutoAttackDamage(minion, true))
                    {
                        Render.Circle.DrawCircle(
                            minion.Position, minion.BoundingRadius * 2/3,
                            Menu.Item("Draw_Lasthit").GetValue<Circle>().Color);
                    }
                    else if (Menu.Item("Draw_nearKill").GetValue<Circle>().Active &&
                             minion.Health <= Player.GetAutoAttackDamage(minion, true) * 2)
                    {
                        Render.Circle.DrawCircle(
                            minion.Position, minion.BoundingRadius,
                            Menu.Item("Draw_nearKill").GetValue<Circle>().Color);
                    }
                }
            }
        }
        private static void Orbwalk(Vector3 goalPosition, Obj_AI_Base target)
        {
            try
            {
                if (target.IsValidTarget() && CanAttack() && IsAllowedToAttack())
                {
                    _disableNextAttack = false;
                    FireBeforeAttack(target);
                    if (!_disableNextAttack)
                    {
                        if (CurrentMode != Mode.Combo)
                        {
                            foreach (
                                var obj in
                                    ObjectManager.Get<Obj_Building>()
                                        .Where(
                                            obj =>
                                                obj.Position.Distance(Player.Position) <=
                                                GetAutoAttackRange() + obj.BoundingRadius / 2 && obj.IsTargetable &&
                                                (obj.Name.StartsWith("Barracks_") || obj.Name.StartsWith("HQ_"))))
                            {
                                Player.IssueOrder(GameObjectOrder.AttackTo, obj.Position);
                                _lastAATick = TickCount + Game.Ping / 2;
                                return;
                            }
                        }
                        if (Player.IssueOrder(GameObjectOrder.AttackUnit, target))
                        {
                            _lastAATick = TickCount + Game.Ping / 2;
                        }
                        return;
                    }
                }
                if (!CanMove() || !IsAllowedToMove())
                {
                    return;
                }
                if (CanMove())
                {
                    if (Menu.Item("Melee_Prediction_Minion").GetValue<bool>() && 
                        Player.IsMelee() && 
                        target != null &&
                        target is Obj_AI_Minion &&
                        !InAutoAttackRange(target) &&                                                
                        Game.CursorPos.Distance(target.Position) < 500)
                    {
                        _movementPrediction.Delay = Player.BasicAttack.SpellCastTime;
                        _movementPrediction.Speed = Player.BasicAttack.MissileSpeed;
                        MoveTo(_movementPrediction.GetPrediction(target).UnitPosition);
                    }
                    else
                    {
                        MoveTo(goalPosition);
                    }
                }
            }
            catch { }
        }
        private static void MoveTo(Vector3 position)
        {
            var delay = Menu.Item("Misc_Humanizer").GetValue<Slider>().Value;
            if (TickCount - _lastMovement < delay)
            {
                return;
            }
            _lastMovement = TickCount;
            if (!CanMove() || !IsAllowedToMove())
            {
                return;
            }

            if (position.Distance(Player.Position) < 500)
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, position);
            }
            else
            {
                var point = Player.ServerPosition + 500 * (position.To2D() - Player.ServerPosition.To2D()).Normalized().To3D();
                Player.IssueOrder(GameObjectOrder.MoveTo, point);
            }
        }
        private static bool IsAllowedToMove()
        {
            if (!_movement)
                return false;
            if (Menu.Item("Misc_AllMovementDisabled").GetValue<bool>())
                return false;
            if (CurrentMode == Mode.Combo && !Menu.Item("Combo_move").GetValue<bool>())
                return false;
            if (CurrentMode == Mode.Harass && !Menu.Item("Harass_move").GetValue<bool>())
                return false;
            if (CurrentMode == Mode.LaneClear && !Menu.Item("LaneClear_move").GetValue<bool>())
                return false;
            if (CurrentMode == Mode.JungleClear && !Menu.Item("JungleClear_move").GetValue<bool>())
                return false;
            if (CurrentMode == Mode.LaneFreeze && !Menu.Item("LaneFreeze_move").GetValue<bool>())
                return false;
            if (CurrentMode == Mode.Custom && !Menu.Item("CustomMode_move").GetValue<bool>() && CustomMode)
                return false;
            return CurrentMode != Mode.Lasthit || Menu.Item("LastHit_move").GetValue<bool>();
        }
        private static bool IsAllowedToAttack()
        {
            if (!_attack)
                return false;
            if (Menu.Item("Misc_AllAttackDisabled").GetValue<bool>())
                return false;
            if (CurrentMode == Mode.Combo && !Menu.Item("Combo_attack").GetValue<bool>())
                return false;
            if (CurrentMode == Mode.Harass && !Menu.Item("Harass_attack").GetValue<bool>())
                return false;
            if (CurrentMode == Mode.LaneClear && !Menu.Item("LaneClear_attack").GetValue<bool>())
                return false;
            if (CurrentMode == Mode.JungleClear && !Menu.Item("JungleClear_attack").GetValue<bool>())
                return false;
            if (CurrentMode == Mode.LaneFreeze && !Menu.Item("LaneFreeze_attack").GetValue<bool>())
                return false;
            if (CurrentMode == Mode.Custom && !Menu.Item("CustomMode_attack").GetValue<bool>() && CustomMode)
                return false;
            return CurrentMode != Mode.Lasthit || Menu.Item("LastHit_attack").GetValue<bool>();

        }
        private static void OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs spell)
        {
            if (IsAutoAttackReset(spell.SData.Name) && unit.IsMe)
            {
                Utility.DelayAction.Add(Menu.Item("Misc_AAReset").GetValue<Slider>().Value, ResetAutoAttackTimer);
            }
            if (!IsAutoAttack(spell.SData.Name))
            {
                return;
            }
            if (unit.IsMe && spell.Target is Obj_AI_Base)
            {
                _lastAATick = TickCount;

                if (spell.Target is Obj_AI_Base)
                {
                    var target = (Obj_AI_Base)spell.Target;
                    if (target.IsValid)
                    {
                        FireOnTargetSwitch(target);
                        _lastTarget = target;
                    }
                }

                if (unit.IsMelee())
                    Utility.DelayAction.Add(
                        (int)(unit.AttackCastDelay * 1000 + 40), () => FireAfterAttack(unit, _lastTarget));
            }
            FireOnAttack(unit, _lastTarget);
        }
        public static Obj_AI_Base GetTarget()
        {
            Obj_AI_Base result = null;

            if (ObjectManager.Get<Obj_Building>()
                    .Any(
                        obj =>
                            obj.Position.Distance(Player.Position) <= GetAutoAttackRange() + obj.BoundingRadius / 2 &&
                            obj.IsTargetable && obj.Name.StartsWith("HQ_")))
                return null;

            Obj_AI_Base tempTarget = null;
            if (CurrentMode == Mode.Harass ||
                (CurrentMode == Mode.LaneClear && !ShouldWait() && Menu.Item("LaneClear_pokes").GetValue<bool>()) ||
                (CurrentMode == Mode.LaneFreeze && !ShouldWait() && Menu.Item("LaneFreeze_pokes").GetValue<bool>()))
            {
                tempTarget = TargetSelector.GetTarget(GetAutoAttackRange(), TargetSelector.DamageType.Physical);
                if (tempTarget.IsValidTarget())
                {
                    if (ObjectManager.Player.UnderTurret(true) && tempTarget.UnderTurret(true) && Menu.Item("Misc_HarassUnderTurret").GetValue<bool>())
                    {
                        return tempTarget;
                    }
                    return tempTarget;
                }
            }
            if (CurrentMode == Mode.Combo && ForcedTarget == null)
            {
                if (LockedTargetSelector._lastTarget != null && LockedTargetSelector._lastTarget.IsValidTarget())
                {
                    tempTarget = LockedTargetSelector._lastTarget;
                    return tempTarget;
                }
                else
                {
                    tempTarget = GetBestHeroTarget();
                    if (tempTarget.IsValidTarget())
                    {
                        return tempTarget;
                    }
                }
                tempTarget = null;
            }
            if (CurrentMode == Mode.Custom && CustomMode)
            {
                if (ForcedTarget.IsValidTarget())
                {
                    return ForcedTarget;
                }
                ForcedTarget = null;
            }
            if (CurrentMode != Mode.Custom && ForcedTarget != null)
            {
                if (ForcedTarget.IsValidTarget() && InAutoAttackRange(ForcedTarget))
                {
                    return ForcedTarget;
                }
                ForcedTarget = null;
            }
            //Kill lowest minion
            if (CurrentMode == Mode.Lasthit || 
                CurrentMode == Mode.LaneClear ||
                (CurrentMode == Mode.LaneFreeze && Menu.Item("LaneFreeze_Lasthit").GetValue<bool>()) ||
                (CurrentMode == Mode.Harass && Menu.Item("Harass_Lasthit").GetValue<bool>()))
            {
                foreach (
                    var minion in
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(
                                minion =>
                                    minion.IsValidTarget() && InAutoAttackRange(minion) &&
                                    minion.Health <
                                    2 *
                                    (ObjectManager.Player.BaseAttackDamage + ObjectManager.Player.FlatPhysicalDamageMod) &&
                                    minion.Name != "Beacon"))
                {
                    var t = (int)(Player.AttackCastDelay * 1000) - 100 + Game.Ping / 2 +
                            1000 * (int)Player.Distance(minion) / (int)MyProjectileSpeed();

                    var predHealth = HealthPrediction.GetHealthPrediction(minion, t, FarmDelay);

                    if (minion.Team != GameObjectTeam.Neutral && MinionManager.IsMinion(minion, true))
                    {
                        if (predHealth < 0)
                        {
                            FireOnNonKillableMinion(minion);
                        }
                        if (predHealth > 0 && predHealth < Player.GetAutoAttackDamage(minion, true))
                        {
                            return minion;
                        }
                    }
                }
            }
            //JungleClear
            if (CurrentMode == Mode.JungleClear)
            {
                result =
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Where(
                            mob => mob.IsValidTarget() && InAutoAttackRange(mob) && mob.Team == GameObjectTeam.Neutral)
                        .MaxOrDefault(mob => mob.MaxHealth);
                if (result != null)
                {
                    if (IsMelee(Player) && !JungleMove) SetMovement(false);
                    return result;
                }
                if (IsMelee(Player) && !JungleMove) SetMovement(true);  
            }           
            //Turrets
            if (CurrentMode == Mode.LaneClear || CurrentMode == Mode.LaneFreeze)
            {
                foreach (
                    var turret in
                        ObjectManager.Get<Obj_AI_Turret>()
                            .Where(turret => turret.IsValidTarget(GetAutoAttackRange(Player, turret))))
                {
                    return turret;
                }
            }
            //Lane clear minions
            float[] maxhealth;
            if (CurrentMode == Mode.LaneClear)
            {
                if (!ShouldWait())
                {
                    if (_prevMinion.IsValidTarget() && InAutoAttackRange(_prevMinion))
                    {
                        var predHealth = HealthPrediction.LaneClearHealthPrediction(
                            _prevMinion, (int)((Player.AttackDelay * 1000) * LaneClearWaitTimeMod), FarmDelay);
                        if (predHealth >= 2 * Player.GetAutoAttackDamage(_prevMinion) ||
                            Math.Abs(predHealth - _prevMinion.Health) < float.Epsilon)
                        {
                            return _prevMinion;
                        }
                    }
                    result = (from minion in
                                  ObjectManager.Get<Obj_AI_Minion>()
                                      .Where(minion => minion.IsValidTarget() && InAutoAttackRange(minion))
                              let predHealth =
                                  HealthPrediction.LaneClearHealthPrediction(
                                      minion, (int)((Player.AttackDelay * 1000) * LaneClearWaitTimeMod), FarmDelay)
                              where
                                  predHealth >= 2 * Player.GetAutoAttackDamage(minion) ||
                                  Math.Abs(predHealth - minion.Health) < float.Epsilon
                              select minion).MaxOrDefault(m => m.Health);

                    if (result != null)
                    {
                        _prevMinion = (Obj_AI_Minion)result;
                    }
                }
            }
            //Target minion with higher HP
            if (CurrentMode == Mode.LaneFreeze)
            {
                maxhealth = new float[] { 0 };
                var maxhealth2 = maxhealth;
                foreach (
                    var minion in
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(
                                minion =>
                                    minion.IsValidTarget(GetAutoAttackRange(Player, minion)) && minion.Name != "Beacon" &&
                                    minion.Team != GameObjectTeam.Neutral)
                            .Where(
                                minion =>
                                    minion.MaxHealth >= maxhealth2[0] ||
                                    Math.Abs(maxhealth2[0] - float.MaxValue) < float.Epsilon))
                {
                    tempTarget = minion;
                    maxhealth[0] = minion.MaxHealth;
                }
                if (tempTarget != null)
                    return tempTarget;
            }

            if (ShouldWait())
            {
                return null;
            }
            return null;
        }
        private static bool ShouldWait()
        {
            return
                ObjectManager.Get<Obj_AI_Minion>()
                    .Any(
                        minion =>
                            minion.IsValidTarget() && minion.Team != GameObjectTeam.Neutral && InAutoAttackRange(minion) &&
                            HealthPrediction.LaneClearHealthPrediction(
                                minion, (int)((Player.AttackDelay * 1000) * LaneClearWaitTimeMod), FarmDelay) <=
                            Player.GetAutoAttackDamage(minion));
        }
        public static float GetMyProjectileSpeed()
        {
            return IsMelee(Player) ? float.MaxValue : Player.BasicAttack.MissileSpeed;
        }
        public static bool IsMelee(Obj_AI_Base unit)
        {
            return unit.CombatType == GameObjectCombatType.Melee;
        }
        public static bool IsAutoAttack(string name)
        {
            return (name.ToLower().Contains("attack") && 
                !NoAttacks.Contains(name.ToLower())) ||
                   Attacks.Contains(name.ToLower());
        }
        public static bool IsWaiting()
        {
            return ShouldWait();
        }
        public static void ResetAutoAttackTimer()
        {
            _lastAATick = 0;
        }
        private static void FireOnNonKillableMinion(AttackableUnit minion)
        {
            if (OnNonKillableMinion != null)
            {
                OnNonKillableMinion(minion);
            }
        }
        public static bool IsAutoAttackReset(string name)
        {
            return AttackResets.Contains(name.ToLower());
        }
        public static bool CanAttack()
        {
            if (_lastAATick <= TickCount)
            {
                return TickCount + Game.Ping / 2 + 25 >= _lastAATick + Player.AttackDelay * 1000 && _attack;
            }
            return false;
        }     
        public static bool CanMove()
        {
            var extraWindup = GetWindUp();
            if (_lastAATick <= TickCount)
                return _movement && NoResets.Contains(ObjectManager.Player.ChampionName) ? (TickCount - _lastAATick > 400) : (TickCount + Game.Ping / 2 >= _lastAATick + Player.AttackCastDelay * 1000 + extraWindup) 
                       ;
            return false;
        }
        private static float MyProjectileSpeed()
        {
            return (Player.CombatType == GameObjectCombatType.Melee) ? float.MaxValue : Player.BasicAttack.MissileSpeed;
        }
        private static int FarmDelay
        {
            get { return Menu.Item("Misc_Farmdelay").GetValue<Slider>().Value; }
        }
        private static bool ImmuneCheck()
        {
            return Menu.Item("ImmuneCheck").GetValue<bool>();
        }
        private static Obj_AI_Base GetBestHeroTarget()
        {

            Obj_AI_Hero killableEnemy = null;
            var hitsToKill = double.MaxValue;
            foreach (var enemy in AllEnemys.Where(
                hero =>
                hero.IsValidTarget() &&
                InAutoAttackRange(hero) &&
                (ImmuneCheck() && !IsImmune(hero))
                ))
            {
                var killHits = CountKillhits(enemy);
                if (killableEnemy != null && (!(killHits < hitsToKill)))

                    continue;
                killableEnemy = enemy;
                hitsToKill = killHits;
            }
            return hitsToKill <= 4
                ? killableEnemy
                : TargetSelector.GetTarget(GetAutoAttackRange(), TargetSelector.DamageType.Physical);
        }
        public static double CountKillhits(Obj_AI_Base enemy)
        {
            return enemy.Health / Player.GetAutoAttackDamage(enemy);
        }
        private static void CheckAutoWindUp()
        {
            if (!Menu.Item("Misc_AutoWindUp").GetValue<bool>())
                return;
            var additional = 0;
            if (Game.Ping >= 100)
                additional = Game.Ping / 100 * 10;
            else if (Game.Ping > 40 && Game.Ping < 100)
                additional = Game.Ping / 100 * 20;
            else if (Game.Ping <= 40)
                additional = +20;
            var windUp = Game.Ping + additional;
            if (windUp < 40)
                windUp = 40;
            Menu.Item("Misc_ExtraWindUp")
                .SetValue(windUp < 200 ? new Slider(windUp, 200, 0) : new Slider(200, 200, 0));
        }
        public static int GetCurrentWindupTime()
        {
            return Menu.Item("Misc_ExtraWindUp").GetValue<Slider>().Value;
        }
        public static float GetAutoAttackRange(Obj_AI_Base source = null, Obj_AI_Base target = null)
        {
            if (source == null)
            {
                source = Player;
            }
            var result = source.AttackRange + source.BoundingRadius;
            if (target != null && target.IsValidTarget())
            {
                result += target.BoundingRadius;
            }
            return result;
        }
        public static bool InAutoAttackRange(Obj_AI_Base target)
        {
            if (!target.IsValidTarget())
            {
                return false;
            }
            var myRange = GetAutoAttackRange(Player, target);
            return
                Vector2.DistanceSquared(
                    (target is Obj_AI_Base) ? ((Obj_AI_Base)target).ServerPosition.To2D() : target.Position.To2D(),
                    Player.ServerPosition.To2D()) <= myRange * myRange;

        }
        public static bool IsImmune(Obj_AI_Base target)
        {
            return (target.HasBuff("JudicatorIntervention") || target.HasBuff("Undying Rage"));
        }
        public static Mode CurrentMode
        {
            get
            {
                if (Menu.Item("Combo_Key").GetValue<KeyBind>().Active)
                    return Mode.Combo;
                if (Menu.Item("Harass_Key").GetValue<KeyBind>().Active)
                    return Mode.Harass;
                if (Menu.Item("LaneClear_Key").GetValue<KeyBind>().Active)
                    return Mode.LaneClear;
                if (Menu.Item("LaneFreeze_Key").GetValue<KeyBind>().Active)
                    return Mode.LaneFreeze;
                if (Menu.Item("JungleClear_Key").GetValue<KeyBind>().Active)
                    return Mode.JungleClear;
                if (Menu.Item("LastHit_Key").GetValue<KeyBind>().Active)
                    return Mode.Lasthit;
                if (Menu.Item("CustomMode_Key").GetValue<KeyBind>().Active && CustomMode)
                    return Mode.Custom;
                return Menu.Item("Flee_Key").GetValue<KeyBind>().Active ? Mode.Flee : Mode.None;
            }
        }
        public static void SetOrbwalkingPoint(Vector3 point)
        {
            _custompoint = point;
        }        
        public static void SetMovement(bool value)
        {
            _movement = value;
        }
        public static bool GetAttack()
        {
            return _attack;
        }
        public static void SetAttack(bool value)
        {
            _attack = value;
        }
        public static void SetForcedTarget(Obj_AI_Hero target)
        {
            ForcedTarget = target;
        }
        public static void UnlockTarget()
        {
            ForcedTarget = null;
        }
        public static bool GetMovement()
        {
            return _movement;
        }
        public static void SetWindUp(int x)
        {
            _duration = x;
        }
        public static int GetWindUp()
        {
            return _duration > 0 ? _duration : Menu.Item("Misc_ExtraWindUp").GetValue<Slider>().Value;
        }
        public class BeforeAttackEventArgs
        {
            public Obj_AI_Base Target;
            public Obj_AI_Base Unit = ObjectManager.Player;
            private bool _process = true;

            public bool Process
            {
                get { return _process; }
                set
                {
                    _disableNextAttack = !value;
                    _process = value;
                }
            }
        }
        private static void FireBeforeAttack(Obj_AI_Base target)
        {
            if (BeforeAttack != null)
            {
                BeforeAttack(new BeforeAttackEventArgs { Target = target });
            }
            else
            {
                _disableNextAttack = false;
            }
        }
        private static void FireOnTargetSwitch(Obj_AI_Base newTarget)
        {
            if (OnTargetChange != null && (_lastTarget == null || _lastTarget.NetworkId != newTarget.NetworkId))
            {
                OnTargetChange(_lastTarget, newTarget);
            }
        }
        private static void FireAfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
            if (AfterAttack != null && target.IsValidTarget())
            {
                AfterAttack(unit, target);
            }
        }
        private static void FireOnAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
            if (OnAttack != null)
            {
                OnAttack(unit, target);
            }
        }
    }
}

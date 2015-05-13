using System;
using System.Linq;
using myOrbwalker;
using LeagueSharp.Common;
using LeagueSharp;
using SharpDX;
using Color = System.Drawing.Color;

namespace Thresh
{
    class Program
    {
        private const string ChampionName = "Thresh";
        private static Menu Config;
        private static Spell Q, W, E, R;
        private static Obj_AI_Hero lastTarget;
        private static Vector3 JumpStart, JumpEnd;
        private static int? JumpTime;
        private const int QTime = 3000;
        private static int QTick;
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName != ChampionName) return;

            Q = new Spell(SpellSlot.Q, 1100);
            W = new Spell(SpellSlot.W, 950);
            E = new Spell(SpellSlot.E, 400);
            R = new Spell(SpellSlot.R, 400);
            Q.SetSkillshot(Q.Instance.SData.SpellCastTime, Q.Instance.SData.LineWidth, Q.Instance.SData.MissileSpeed, true, SkillshotType.SkillshotLine);

            Config = new Menu("Thresh", "Thresh", true);
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
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQFollowUp", "Q Leap").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseWComboType", "W").SetValue(new StringList(new[] { "Path Block", "Ally", "Self" })));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseWComboLogic", "W After Q").SetValue(new StringList(new[] { "Always", "Hooked", "Leaped" })));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseEComboType", "E").SetValue(new StringList(new[] { "Inwards", "Outwards" })));
            Config.SubMenu("Combo").AddItem(new MenuItem("RComboValue", "R More Than").SetValue(new Slider(1, 1, 4)));
            Config.SubMenu("Combo").AddItem(new MenuItem("TurretDive", "Turret Dive").SetValue(false));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            
            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseEFarm", "Use E").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("EFarmValue", "E More Than").SetValue(new Slider(1, 1, 5)));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseEFarmType", "E").SetValue(new StringList(new[] { "Inwards", "Outwards" })));
            Config.SubMenu("Farm").AddItem(new MenuItem("FarmMana", "Farm Mana >").SetValue(new Slider(50)));

            Config.AddSubMenu(new Menu("Jungle Farm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(true));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("QPredLogic", "Q Logic").SetValue(new StringList(new[] { "1", "2", "3" })));
            Config.SubMenu("Misc").AddItem(new MenuItem("QPredHitchance", "Q Hitchance").SetValue(new StringList(new[] { "Low", "Medium", "High" })));
            Config.SubMenu("Misc").AddItem(new MenuItem("UseEGap", "E Gap Closer").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("UseQInterruptSpells", "Q Interrupts").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("UseEInterruptSpells", "E Interrupts").SetValue(true));
            
            Config.AddToMainMenu();

            Obj_AI_Base.OnProcessSpellCast += Game_OnProcessSpell;
            Game.OnUpdate += OnUpdate;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            AntiGapcloser.OnEnemyGapcloser -= OnEnemyGapcloser;
            MYOrbwalker.OnNonKillableMinion += OnNonKillableMinion;
            Spellbook.OnCastSpell += OnCastSpell;
            CustomEvents.Unit.OnDash += OnDash;
            Drawing.OnEndScene += Drawing_OnEndScene;
        }
        private static Obj_AI_Hero LastQTarget { get; set; }
        private static void Drawing_OnEndScene(EventArgs args)
        {
            if (ObjectManager.Player.IsDead) return;
            var target = MYOrbwalker.ComboLocked ? LockedTargetSelector._lastTarget : MYOrbwalker.GetEnemyChampion();            
            if (target != null) Render.Circle.DrawCircle(target.Position, 125, Color.Lime, 7, true);
           
        }
        private static void OnDash(Obj_AI_Base sender, Dash.DashItem args)
        {
            if (sender.IsMe)
            {
                JumpStart = args.StartPos.To3D();
                JumpEnd = args.EndPos.To3D();
                JumpTime = Environment.TickCount;
            }
        }
        private static void OnNonKillableMinion(AttackableUnit minion)
        {
            if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.LaneClear && E.IsReady())
            {
                if (!Config.Item("UseEFarm").GetValue<bool>()) return;
                if (GetPlayerManaPercentage() < Config.Item("FarmMana").GetValue<Slider>().Value) return;
                var target = minion as Obj_AI_Minion;                
                if (target != null && 
                    E.IsKillable(target) && 
                    Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) < E.Range && 
                    !ObjectManager.Player.IsWindingUp && 
                    !MYOrbwalker.IsWaiting() &&
                    target.BaseSkinName.Contains("Siege"))
                {
                   E.Cast(EInwards(ObjectManager.Player, target.Position));
                }
            }
        }
        private static void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Config.Item("UseEGap").GetValue<bool>() && E.IsInRange(gapcloser.End))
                E.Cast(gapcloser.End.To2D());
        }
        private static void Game_OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs spell)
        {
            if (unit.IsMe)
            {
                if (spell.SData.Name.ToLower() == "threshq")
                {
                    QTick = Environment.TickCount;                    
                }
            }
        }
        private static void OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (sender.Owner.IsMe && args.Slot == SpellSlot.E && ObjectManager.Player.IsDashing())
            {
                args.Process = false;                
            }
        }
        private static void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (Config.Item("UseQInterruptSpells").GetValue<bool>() && Q.IsInRange(sender) && args.DangerLevel >= Interrupter2.DangerLevel.High)
            {
                QPred(sender);
            }
            if (Config.Item("UseEInterruptSpells").GetValue<bool>() && E.IsInRange(sender))
            {           
                E.Cast(sender.ServerPosition.To2D());
            }            
        }
        private static void OnUpdate(EventArgs args)
        {
            QReset();
            if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.None)
            {
                LockedTargetSelector.UnlockTarget();
            }
            if (ObjectManager.Player.IsDead) return;            
            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active) LaneClear();
            if (Config.Item("JungleActive").GetValue<KeyBind>().Active) JungleClear();
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active) Combo();
            if (Config.Item("HarassActive").GetValue<KeyBind>().Active) Harass();            
        }
        private static void LaneClear()
        {
            if (GetPlayerManaPercentage() < Config.Item("FarmMana").GetValue<Slider>().Value) return;
            if (Config.Item("UseEFarm").GetValue<bool>() && E.IsReady() && !ObjectManager.Player.IsWindingUp && !MYOrbwalker.IsWaiting())
            {
                var AllMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).ToList();
                foreach (var x in AllMinionsE)
                {
                    var InE = MinionManager.GetMinions(x.ServerPosition, 250f).Count();
                    if (InE > Config.Item("EFarmValue").GetValue<Slider>().Value)
                    {
                        if (x.ServerPosition.UnderTurret(true))
                        {
                            E.Cast(EInwards(ObjectManager.Player, x.ServerPosition));
                        }
                        else if (x.ServerPosition.UnderTurret(false))
                        {
                            E.Cast(x.ServerPosition);
                        }
                        else
                        {
                            switch (Config.Item("UseEFarmType").GetValue<StringList>().SelectedIndex)
                            {
                                case 0:
                                    E.Cast(EInwards(ObjectManager.Player, x.ServerPosition));
                                    break;
                                case 1:
                                    E.Cast(x.ServerPosition);
                                    break;
                            }
                        }
                    }
                }            
            }
        }
        private static void JungleClear()
        {
            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth).FirstOrDefault();
            var largemobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.Health).FirstOrDefault(x => !x.BaseSkinName.Contains("Mini"));
            if (Config.Item("UseQJFarm").GetValue<bool>() && Q.IsReady() && Environment.TickCount - QTick >= QTime && !ObjectManager.Player.IsWindingUp && !QLeap())
            {
                if (largemobs != null && Q.IsInRange(largemobs) && Q.IsKillable(largemobs) && !ObjectManager.Player.IsWindingUp)
                {
                    Q.Cast(largemobs.ServerPosition);
                }
            }
            if (Config.Item("UseEJFarm").GetValue<bool>() && E.IsReady() && !ObjectManager.Player.IsWindingUp)
            {
                if (largemobs != null && E.IsInRange(largemobs))
                {
                    E.Cast(largemobs.ServerPosition);
                }
                else 
                {
                    if (mobs != null && mobs.IsValidTarget() && E.IsInRange(mobs)) 
                        E.Cast(EInwards(ObjectManager.Player, mobs.ServerPosition));
                }
            }
        }
        private static void Combo()
        {
            Obj_AI_Hero target;
            if (MYOrbwalker.ComboLocked)
            {
                target = LockedTargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            }
            else
            {
                target = MYOrbwalker.GetEnemyChampion();
            }
            var UseQ = Config.Item("UseQCombo").GetValue<bool>();
            var UseW = Config.Item("UseWCombo").GetValue<bool>();
            var UseE = Config.Item("UseECombo").GetValue<bool>();
            var UseR = Config.Item("UseRCombo").GetValue<bool>();
            var UseQFollowUp = Config.Item("UseQFollowUp").GetValue<bool>();
            if (target.IsValidTarget())
            {
                try
                {
                    if (UseQ && Q.IsReady() && Environment.TickCount - QTick >= QTime && !ObjectManager.Player.IsWindingUp && !QLeap())
                    {
                        QPred(target);           
                    }
                    if (UseQFollowUp && target == LastQTarget && (Environment.TickCount - QTick > 1000 && Environment.TickCount - QTick < 3000) && target.HasBuff("ThreshQ") && !target.HasBuff("threshqfakeknockup") && !ObjectManager.Player.IsWindingUp)
                    {
                        if (target.UnderTurret(true) && !Config.Item("TurretDive").GetValue<bool>()) return;
                        Q.Cast();
                    }
                    if (UseW && W.IsReady() && !ObjectManager.Player.IsWindingUp)
                    {
                        switch (Config.Item("UseWComboType").GetValue<StringList>().SelectedIndex)
                        {
                            case 0:
                               //path block
                                if (Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) <= 900)
                                {
                                    switch (Config.Item("UseWComboLogic").GetValue<StringList>().SelectedIndex)
                                    {
                                        case 0:
                                            W.Cast(ObjectManager.Player.ServerPosition.Extend(target.ServerPosition, 125f));
                                            break;
                                        case 1:
                                            if (target.HasBuff("ThreshQ"))
                                            {
                                                W.Cast(ObjectManager.Player.ServerPosition.Extend(target.ServerPosition, 125f));
                                            }
                                            break;
                                        case 2:
                                            if (target == LastQTarget && Environment.TickCount - QTick < 3000 && target.HasBuff("ThreshQ") && !target.HasBuff("threshqfakeknockup"))
                                            {
                                                W.Cast(ObjectManager.Player.ServerPosition.Extend(target.ServerPosition, 125f));
                                            }
                                            break;
                                    }
                                }
                                break;
                            case 1:
                             //friendly
                                var Ally = ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsAlly && !x.IsDead && !x.IsMe && Vector3.Distance(ObjectManager.Player.ServerPosition, x.ServerPosition) <= 925 + 200).OrderByDescending(i => i.Distance(ObjectManager.Player)).FirstOrDefault();
                                if (Ally != null)
                                {
                                    switch (Config.Item("UseWComboLogic").GetValue<StringList>().SelectedIndex)
                                    {
                                        case 0:
                                            W.Cast(ObjectManager.Player.ServerPosition.Extend(Ally.ServerPosition, 100f));
                                            break;
                                        case 1:
                                            if (target.HasBuff("ThreshQ"))
                                            {
                                                W.Cast(ObjectManager.Player.ServerPosition.Extend(Ally.ServerPosition, 100f));
                                            }
                                            break;
                                        case 2:
                                            if (target == LastQTarget && Environment.TickCount - QTick < 3000 && target.HasBuff("ThreshQ") && !target.HasBuff("threshqfakeknockup"))
                                            {
                                                W.Cast(ObjectManager.Player.ServerPosition.Extend(Ally.ServerPosition, 100f));
                                            }
                                            break;
                                    }
                                }               
                                break;
                            case 2:
                                //always                                
                                switch (Config.Item("UseWComboLogic").GetValue<StringList>().SelectedIndex)
                                {
                                    case 0:
                                        W.Cast(ObjectManager.Player.ServerPosition.Extend(target.ServerPosition, (Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) / 2)));
                                        break;
                                    case 1:
                                        if (target.HasBuff("ThreshQ"))
                                        {
                                            W.Cast(ObjectManager.Player.ServerPosition.Extend(target.ServerPosition, (Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) / 2)));
                                        }
                                        break;
                                    case 2:
                                        if (target == LastQTarget && Environment.TickCount - QTick < 3000 && target.HasBuff("ThreshQ") && !target.HasBuff("threshqfakeknockup"))
                                        {
                                            W.Cast(ObjectManager.Player.ServerPosition.Extend(target.ServerPosition, (Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) / 2)));
                                        }
                                        break;                                    
                                }
                                break;
                        }
                    }
                    if (UseE && E.IsReady() && E.IsInRange(target) && !ObjectManager.Player.IsDashing() && !ObjectManager.Player.IsWindingUp && !target.HasBuff("ThreshQ") && (Environment.TickCount - QTick > 2000))
                    {
                        switch (Config.Item("UseEComboType").GetValue<StringList>().SelectedIndex)
                        {
                            case 0:
                                if (Environment.TickCount - JumpTime < (1000 + Game.Ping))
                                {
                                    E.Cast(JumpStart);
                                }
                                else
                                {
                                    E.Cast(EInwards(ObjectManager.Player, target.Position));
                                }
                                break;
                            case 1:
                                if (Environment.TickCount - JumpTime < (1000 + Game.Ping))
                                {
                                    E.Cast(JumpEnd);
                                }
                                else
                                {
                                    E.Cast(target.Position);
                                }
                                break;
                        }                        
                    }
                    if (UseR && R.IsReady() && !ObjectManager.Player.IsWindingUp)
                    {
                        var EnemyList = HeroManager.AllHeroes.Where(x => x.IsValidTarget() && x.IsEnemy && !x.IsDead && !x.IsZombie && !x.IsInvulnerable);
                        var ValidTargets = EnemyList.Where(x => !x.InFountain() && x.IsVisible && Vector3.Distance(ObjectManager.Player.ServerPosition, x.ServerPosition) < R.Range).ToList();
                        if (ValidTargets.Count() == 1 && ValidTargets[0] != null)
                        {
                            if (ValidTargets[0].HealthPercent < 50) R.Cast();
                        }
                        else if (ValidTargets.Count() > Config.Item("RComboValue").GetValue<Slider>().Value)
                        {
                            R.Cast();
                        }
                        
                    }
                }
                catch { }
            }
        }
        private static void Harass()
        {
            var UseQ = Config.Item("UseQHarass").GetValue<bool>();
            var UseE = Config.Item("UseEHarass").GetValue<bool>();
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (target.IsValidTarget())
            {
                try
                {
                    if (UseQ && Q.IsReady() && Environment.TickCount - QTick >= QTime && !ObjectManager.Player.IsWindingUp && !QLeap() && Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) < Q.Range - 70f)
                    {
                        QPred(target);                        
                    }
                    if (UseE && E.IsReady() && E.IsInRange(target) && !ObjectManager.Player.IsDashing() && !ObjectManager.Player.IsWindingUp)
                    {
                        E.Cast(target.Position);
                    }
                }
                catch { }
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
        private static void QReset()
        {
            if (LastQTarget != null)
            {
                if (Environment.TickCount - QTick > QTime)
                {
                    LastQTarget = null;
                }
            }
        }
        private static bool QLeap()
        {
            return Q.Instance.Name == "threshqleap";
        }
        private static void QPred(Obj_AI_Hero target)
        {             
            PredictionOutput pred = Q.GetPrediction(target);
            if (pred.Hitchance >= QHitChance && pred.CollisionObjects.Count == 0 &&                
                Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) < Q.Range)
            {
                
                    switch (Config.Item("QPredLogic").GetValue<StringList>().SelectedIndex)
                    {
                        case 0:
                           Q.Cast(target.ServerPosition);                           
                            break;
                        case 1:
                            var vc1 = new Vector3(
                                target.ServerPosition.X + ((pred.UnitPosition.X - target.ServerPosition.X) / 2),
                                target.ServerPosition.Y + ((pred.UnitPosition.Y - target.ServerPosition.Y) / 2),
                                target.ServerPosition.Z);
                            Q.Cast(vc1);
                           
                            break;
                        case 2:
                            Q.Cast(target.ServerPosition.Extend(pred.CastPosition, 75f));
                            break;
                            
                    }
                    LastQTarget = target;                                                
            }
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
        private static Vector3 EInwards(Obj_AI_Hero source, Vector3 target)
        {
            return new Vector3(source.Position.X + (source.Position.X - target.X), source.Position.Y + (source.Position.Y - target.Y), source.Position.Z);
        }

    }
}

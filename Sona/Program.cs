using System;
using System.Collections.Generic;
using System.Linq;
using myOrbwalker;
using LeagueSharp.Common;
using LeagueSharp;
using SharpDX;

namespace Sona
{
    class Program
    {
        private const string ChampionName = "Sona";
        private static Menu Config;
        private static Spell Q, W, E, R;
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        private static void Game_OnGameLoad(EventArgs args)
        {            
            if (ObjectManager.Player.ChampionName != ChampionName) return;
            Q = new Spell(SpellSlot.Q, 850f);
            W = new Spell(SpellSlot.W, 1000f);
            E = new Spell(SpellSlot.E, 350f);
            R = new Spell(SpellSlot.R, 1000f);
            R.SetSkillshot(0.2f, 125, float.MaxValue, false, SkillshotType.SkillshotLine);

            Config = new Menu("Sona", "Sona", true);
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
            Config.SubMenu("Keys").AddItem(new MenuItem("JungActive", "Jungle Clear").SetValue(new KeyBind(Config.Item("JungleClear_Key").GetValue<KeyBind>().Key, KeyBindType.Press))); //v
            Config.SubMenu("Keys").AddItem(new MenuItem("FleeActive", "Flee").SetValue(new KeyBind(Config.Item("Flee_Key").GetValue<KeyBind>().Key, KeyBindType.Press)));  //space

            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(false));
            Config.SubMenu("Combo").AddItem(new MenuItem("SheenCheck", "Sheen Check").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRType", "R").SetValue(new StringList(new[] { "Always", "Multi", "Killable" })));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassManaFreeze", "Harass Mana >").SetValue(new Slider(50)));

            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("FarmMana", "Farm Mana >").SetValue(new Slider(50)));

            Config.AddSubMenu(new Menu("Misc Manager", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("FountainPowerChord", "Ready Power Chord").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("AutoShield", "Auto Shield").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("AutoHeal", "Auto Heal").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("HealMinimum", "Keep HP >").SetValue(new Slider(70)));
            Config.SubMenu("Misc").AddItem(new MenuItem("SmartHeal", "Smart Mode").SetValue(true));
            
            Config.AddToMainMenu();

            AntiGapcloser.OnEnemyGapcloser -= OnEnemyGapcloser;
            Obj_AI_Base.OnProcessSpellCast += Game_OnProcessSpell;
            //MYOrbwalker.AfterAttack += OnAfterAttack;
            //Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Game.OnUpdate += Game_OnUpdate;
            MYOrbwalker.OnNonKillableMinion += OnNonKillableMinion;
            Spellbook.OnCastSpell += OnCastSpell;
        }
        private static void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (ObjectManager.Player.Distance(gapcloser.Sender) <= ObjectManager.Player.AttackRange)
            {
                if (PowerChordIsReady() || PowerChordCount() == 2)
                {
                    if (E.IsReady() || !W.IsReady()) E.Cast();
                    else if (!E.IsReady() || W.IsReady()) W.Cast();
                    ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, gapcloser.Sender);
                }
            }
        }        
        private static void Game_OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs spell)
        {
            if (!unit.IsMe)
            {
                if (!unit.IsEnemy)
                {
                    return;
                }
                if (Config.Item("AutoShield").GetValue<bool>() && (unit.Type == GameObjectType.obj_AI_Turret) || unit.IsValid<Obj_AI_Hero>())
                {                   
                    if (spell.Target.IsMe)
                    {
                        W.Cast();
                    }
                    return;
                }
            }
        }
        private static void OnAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            
        }
        private static void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (Config.Item("InterruptSpells").GetValue<bool>())
            {
                if (ObjectManager.Player.Distance(sender) < R.Range && R.IsReady())
                {
                    //NOT SURE IF WANT TO WASTE ULT
                }
            }
        }
        private static void OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (sender.Owner.IsMe && (args.Slot == SpellSlot.Q || args.Slot == SpellSlot.W || args.Slot == SpellSlot.E || args.Slot == SpellSlot.R) && 
                (ObjectManager.Player.HasBuff("Sheen") || ObjectManager.Player.HasBuff("lichbane")) &&
                Config.Item("SheenCheck").GetValue<bool>())
            {
                if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Combo)
                {
                    if (args.Target is Obj_AI_Hero && args.Target.IsEnemy && Orbwalking.InAutoAttackRange((Obj_AI_Hero)args.Target))
                    {
                        args.Process = false;
                        ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, args.Target);
                    }
                }
            }                        
        }
        private static void OnNonKillableMinion(AttackableUnit minion)
        {
            if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.LaneClear)
            {
                if (Config.Item("UseQFarm").GetValue<bool>() && GetPlayerManaPercentage() > Config.Item("FarmMana").GetValue<Slider>().Value)
                {
                    var target = minion as Obj_AI_Minion;
                    if (target != null && Q.IsKillable(target) && Orbwalking.InAutoAttackRange(target) && Q.IsReady() && !ObjectManager.Player.IsWindingUp)
                    {
                        Q.Cast();
                    }
                }
            }
        }
        private static void Game_OnUpdate(EventArgs args)
        {
            if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.None)
            {
                AutoPowerChord();
                AutoHeal();
                LockedTargetSelector.UnlockTarget();
            }
            if (ObjectManager.Player.IsDead) return;            
            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active) LaneClear();
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active) Combo(); 
            if (Config.Item("HarassActive").GetValue<KeyBind>().Active) Harass();
           
        }
        private static void LaneClear()
        {
            if (GetPlayerManaPercentage() < Config.Item("FarmMana").GetValue<Slider>().Value) return;
            if (Config.Item("UseQFarm").GetValue<bool>() && Q.IsReady())
            {
                var MinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).OrderBy(i => i.Distance(ObjectManager.Player)).ToList();
                var EnemyQ = ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy && !enemy.IsDead && enemy.IsValidTarget() && Vector3.Distance(ObjectManager.Player.ServerPosition, enemy.ServerPosition) <= Q.Range).ToList();                
                switch (EnemyQ.Count())
                {
                    case 0:
                        if (MinionsQ[0].IsValidTarget() && MinionsQ[1].IsValidTarget())
                        {
                            if (Q.IsKillable(MinionsQ[0]) && Q.IsKillable(MinionsQ[1]))
                            {
                                Q.Cast(); 
                            }
                            else if (Q.IsKillable(MinionsQ[0]) || Q.IsKillable(MinionsQ[1]))
                            {
                                Q.Cast();
                            }                            
                        }
                        break;
                    case 1:
                        if (MinionsQ[0].IsValidTarget() && !ObjectManager.Player.UnderTurret(true))
                        {
                            if (Q.IsKillable(MinionsQ[0]))
                            {
                                Q.Cast();
                            }
                        }
                        break;
                    default:
                        return;                 
                }
            }
        }
        private static void Combo ()
        {
            var target = LockedTargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            var UseQ = Config.Item("UseQCombo").GetValue<bool>();
            var UseE = Config.Item("UseECombo").GetValue<bool>();
            var UseR = Config.Item("UseRCombo").GetValue<bool>();            
            if (target.IsValidTarget())
            {
                if (UseQ && Q.IsReady())
                {
                    if (Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) < Q.Range)
                    {
                        Q.Cast();
                    }
                }
                if (UseE)
                {
                    EChase(target);
                }
                if (UseR)
                {
                    RPredict(target);
                }
            }
        }
        private static void Harass()
        {
            if (GetPlayerManaPercentage() < Config.Item("HarassManaFreeze").GetValue<Slider>().Value) return;
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (target.IsValidTarget() && Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) < Q.Range && Q.IsReady())
            {
                Q.Cast();
            }
        }
        private static void AutoHeal()
        {
            if (!Config.Item("AutoHeal").GetValue<bool>() || ObjectManager.Player.InFountain() || ObjectManager.Player.InShop() || ObjectManager.Player.HasBuff("Recall") || ObjectManager.Player.IsWindingUp) return;
            if (GetPlayerManaPercentage() < 33) return;
            if (Config.Item("SmartHeal").GetValue<bool>())
            {
                double wHeal = (10 + 20 * W.Level + .2 * ObjectManager.Player.FlatMagicDamageMod) * (1 + (1 - (ObjectManager.Player.Health / ObjectManager.Player.MaxHealth)) / 2);
                if (ObjectManager.Player.MaxHealth - ObjectManager.Player.Health > wHeal) W.Cast();
            }
            else if (GetPlayerHealthPercentage() < Config.Item("HealMinimum").GetValue<Slider>().Value) W.Cast();
        }
        private static void AutoPowerChord()
        {
            if (Config.Item("FountainPowerChord").GetValue<bool>() && !PowerChordIsReady() && !ObjectManager.Player.InShop() && ObjectManager.Player.InFountain())
            {
                switch (PowerChordCount())
                {
                    case 0:
                        if (E.IsReady()) E.Cast();
                        if (W.IsReady()) W.Cast();
                        if (Q.IsReady()) Q.Cast();
                        break;
                    case 1:
                        if (W.IsReady()) W.Cast();
                        if (Q.IsReady()) Q.Cast();
                        break;
                    case 2:
                        if (Q.IsReady()) Q.Cast();
                        break;
                }
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
        private static bool PowerChordIsReady()
        {
            return ObjectManager.Player.HasBuff("sonapassiveattack");
        }
        private static int PowerChordCount()
        {
            foreach (var buffs in ObjectManager.Player.Buffs.Where(buffs => buffs.Name == "sonapassivecount"))
            {
                return buffs.Count;
            }
            return 0;
        }
        private static void EChase(Obj_AI_Base target)
        {
            if (!E.IsReady()) return;
            try
            {
                if (target.Path.Length == 0 || !target.IsMoving) return;
                Vector2 nextEnemPath = target.Path[0].To2D();
                var dist = ObjectManager.Player.Position.To2D().Distance(target.Position.To2D());
                var distToNext = nextEnemPath.Distance(ObjectManager.Player.Position.To2D());
                if (distToNext <= dist)
                    return;
                var msDif = ObjectManager.Player.MoveSpeed - target.MoveSpeed;
                if (msDif <= 0) E.Cast();
                var reachIn = dist / msDif;
                if (reachIn > 1) E.Cast();
            }
            catch { }
        }
        private static void RPredict(Obj_AI_Base target)
        {
            if (!R.IsReady()) return;
            var nearChamps = (from champ in ObjectManager.Get<Obj_AI_Hero>() where champ.IsValidTarget(R.Range) && target != champ select champ).ToList();
            if (ObjectManager.Player.Distance(target) < R.Range)
            {
                switch (Config.Item("UseRType").GetValue<StringList>().SelectedIndex)
                {
                    //alawys
                    case 0:
                        R.Cast(R.GetPrediction(target).CastPosition);
                        break;
                    //multi
                    case 1:
                        PredictionOutput prediction = R.GetPrediction(target);
                        if (nearChamps.Count > 0)
                        {
                            var closeToPrediction = new List<Obj_AI_Hero>();
                            foreach (var enemy in nearChamps)
                            {
                                prediction = R.GetPrediction(enemy);
                                if (prediction.Hitchance == HitChance.High && (ObjectManager.Player.Distance(enemy) < R.Range))
                                    closeToPrediction.Add(enemy);
                            }
                            if (closeToPrediction.Count > 0)
                            {
                                R.Cast(prediction.CastPosition);
                            }
                        }
                        break;
                    //killable
                    case 2:
                        if (R.IsKillable(target))
                        {
                            R.Cast(R.GetPrediction(target).CastPosition);
                        }
                        break;
                }
            }
        }                
    }
}

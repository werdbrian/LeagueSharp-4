using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using myOrbwalker;
using LeagueSharp.Common.Data;
using LeagueSharp.Common;
using LeagueSharp;
using SharpDX;
using Color = System.Drawing.Color;

namespace Kayle
{
    class Program
    {
        private const string ChampionName = "Kayle";
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
            Q = new Spell(SpellSlot.Q, 650f);
            W = new Spell(SpellSlot.W, 900f);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 900f);

            Q.SetTargetted(0.5f, Q.Instance.SData.SpellCastTime);
            W.SetTargetted(0.5f, W.Instance.SData.SpellCastTime);
            E.SetTargetted(0.5f, E.Instance.SData.SpellCastTime);
            R.SetTargetted(0.5f, R.Instance.SData.SpellCastTime);

            Config = new Menu("Kayle", "Kayle", true);
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
            Config.SubMenu("Combo").AddItem(new MenuItem("Debuff", "Debuff Count").SetValue(new Slider(2, 0, 5)));
            //Config.SubMenu("Combo").AddItem(new MenuItem("TurretDive", "Turret Dive").SetValue(false));
            //Config.SubMenu("Combo").AddItem(new MenuItem("UseItemCombo", "Use Items").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("Sticky", "Stick to Target").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("StickyType", "Type").SetValue(new StringList(new[] { "AA Range", "Slider" })));
            Config.SubMenu("Combo").AddItem(new MenuItem("StickyTypeSlider", "Range").SetValue(new Slider(50, 50, 1000)));
            
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            //Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            
            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(false));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseEFarm", "Use E").SetValue(false));
            Config.SubMenu("Farm").AddItem(new MenuItem("EFarmValue", "E More Than").SetValue(new Slider(1, 1, 5)));
            Config.SubMenu("Farm").AddItem(new MenuItem("FarmMana", "Farm Mana >").SetValue(new Slider(50)));

            Config.AddSubMenu(new Menu("Jungle Farm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(true));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            //Config.SubMenu("Misc").AddItem(new MenuItem("AutoR", "Auto R").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("UseRKey", "Intervention (R) Key").SetValue(new KeyBind(Config.Item("CustomMode_Key").GetValue<KeyBind>().Key, KeyBindType.Press)));  //T
            //Config.SubMenu("Misc").AddItem(new MenuItem("UseRType", "R").SetValue(new StringList(new[] { "Self", "Ally" })));
            Config.SubMenu("Misc").AddItem(new MenuItem("AutoHeal", "Auto Heal").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("HealMinimum", "Keep HP >").SetValue(new Slider(50)));
            Config.SubMenu("Misc").AddItem(new MenuItem("SmartMode", "Smart Mode").SetValue(true));

            Config.AddToMainMenu();
            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnEndScene += Drawing_OnEndScene;
        }
        private static void Drawing_OnEndScene(EventArgs args)
        {
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
            if (Config.Item("UseRKey").GetValue<KeyBind>().Active) Intervention();        
            AutoHeal();                       
        }
        private static void Combo()
        {
            //var target = LockedTargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
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
            var Sticks = Config.Item("Sticky").GetValue<bool>();
            if (Sticks)
            {
                SetOrbwalkingToTarget(target);
            }
            if (target.IsValidTarget())
            {
                if (target.InFountain()) return;
                try
                {
                    if (UseQ && Q.IsReady() && Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) <= Q.Range)
                    {
                        if (!ObjectManager.Player.IsWindingUp && DebuffCount(target) >= Config.Item("Debuff").GetValue<Slider>().Value)
                        {
                            Q.CastOnUnit(target);
                        }
                    }
                    if (UseW)
                    {
                        WChase(target);
                    }
                    if (UseE && E.IsReady() && Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) <= 500f)
                    {
                        E.CastOnUnit(ObjectManager.Player);
                    }
                }
                catch { }
            }
            else SetOrbwalkToDefault();       
        }
        private static void Harass()
        {
            var UseQ = Config.Item("UseQHarass").GetValue<bool>();
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (target.IsValidTarget())
            {
                if (UseQ && Q.IsReady() && Q.IsInRange(target))
                {
                    Q.CastOnUnit(target);
                }
            }
        }
        private static void LaneClear()
        {
            if (GetPlayerManaPercentage() < Config.Item("FarmMana").GetValue<Slider>().Value) return;            
            if (Config.Item("UseQFarm").GetValue<bool>() && Q.IsReady() && !ObjectManager.Player.IsWindingUp)
            {
                var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health);
                foreach (var minionq in allMinionsQ)
                {
                    if (minionq.Health < ObjectManager.Player.GetSpellDamage(minionq, SpellSlot.Q) && !ObjectManager.Player.IsWindingUp && !minionq.UnderTurret(true) && (!Orbwalking.InAutoAttackRange(minionq) && !EBuff()))
                    {
                        Q.Cast(minionq);                     
                    }
                }
            }
            if (Config.Item("UseEFarm").GetValue<bool>() && E.IsReady() && !EBuff() && !ObjectManager.Player.IsWindingUp)
            {
                //var minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, 525f, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).OrderByDescending(i => i.Distance(ObjectManager.Player)).ToList();

                //if (minions.Any() && !ObjectManager.Player.IsWindingUp)
                //{
                //    E.CastOnUnit(ObjectManager.Player);
                //}
                var epos = GetEPos();
                if (epos.Item1 == 0) return;
                if (epos.Item1 > Config.Item("EFarmValue").GetValue<Slider>().Value)
                {
                    E.CastOnUnit(ObjectManager.Player);
                }
            }
        }
        private static void JungleClear()
        {
            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            var largemobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.Health).FirstOrDefault(x => !x.BaseSkinName.Contains("Mini"));
            if (Config.Item("UseQJFarm").GetValue<bool>() && Q.IsReady() && !ObjectManager.Player.IsWindingUp)
            {
                if (largemobs != null && Q.IsKillable(largemobs) && !EBuff())
                {
                    Q.Cast(largemobs);
                }
                else if (mobs[0] != null)
                {
                    Q.Cast(mobs[0]);
                }
            }
            if (Config.Item("UseEJFarm").GetValue<bool>() && E.IsReady() && !EBuff() && !ObjectManager.Player.IsWindingUp)
            {
                if (mobs[0] != null) E.CastOnUnit(ObjectManager.Player);
            }
        }
        private static void Intervention()
        {
        
        }
        private static void AutoHeal()
        {
            if (!Config.Item("AutoHeal").GetValue<bool>()) return;
            if (GetPlayerManaPercentage() < 10 && GetPlayerHealthPercentage() > 50) return;
            if (ObjectManager.Player.InFountain() || ObjectManager.Player.InShop() || ObjectManager.Player.HasBuff("Recall") || ObjectManager.Player.IsWindingUp) return;
            if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.None)
            {
                if (Config.Item("SmartMode").GetValue<bool>())
                {
                    var wHeal = (15 + 45 * W.Level + .45 * ObjectManager.Player.FlatMagicDamageMod) * (1 + (1 - (ObjectManager.Player.Health / ObjectManager.Player.MaxHealth)) / 2);
                    if (ObjectManager.Player.MaxHealth - ObjectManager.Player.Health > wHeal) W.CastOnUnit(ObjectManager.Player);
                }
                else if (GetPlayerHealthPercentage() < Config.Item("HealMinimum").GetValue<Slider>().Value) W.CastOnUnit(ObjectManager.Player);
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
        private static void WChase(Obj_AI_Base target)
        {
            if (!W.IsReady() && !target.IsValidTarget()) return;
            try
            {
                var dist = ObjectManager.Player.Position.To2D().Distance(target.Position.To2D());
                var msDif = ObjectManager.Player.MoveSpeed - target.MoveSpeed;
                var reachIn = dist / msDif;
                if (reachIn > 4 && W.IsReady() && !ObjectManager.Player.IsWindingUp)
                {
                    W.Cast();
                }
            }
            catch { }
        }
        private static bool EBuff()
        {
            return ObjectManager.Player.HasBuff("JudicatorRighteousFury");
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
        private static int DebuffCount(Obj_AI_Base target)
        {
            return target.Buffs.Count(x => x.Name == "judicatorholyfervordebuff");            
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

    }
}
//125 melee
//525 range
//JudicatorRighteousFury
//JudicatorReckoning
//judicatorholyfervordebuff 
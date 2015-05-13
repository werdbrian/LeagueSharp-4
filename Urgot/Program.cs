using System;
using System.Collections.Generic;
using System.Linq;
using myOrbwalker;
using LeagueSharp.Common;
using LeagueSharp;
using LeagueSharp.Common.Data;
using SharpDX;

namespace Urgot
{
    class Program
    {
        private const string ChampionName = "Urgot";
        private static Menu Config;
        private static bool IsChanneling;
        private static Spell Q, QLockOn, W, E, R;
        private static Obj_AI_Hero MuramanaTarget;
        private static Obj_AI_Base Qminion;
        private static Dictionary<Obj_SpellMissile, Obj_AI_Hero> objectlist;
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName != ChampionName) return;

            Q = new Spell(SpellSlot.Q, 975f);
            QLockOn = new Spell(SpellSlot.Q, 1200f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 900);
            R = new Spell(SpellSlot.R); //550, 700, 850           

            Q.SetSkillshot(0.2667f, 60f, 1600f, true, SkillshotType.SkillshotLine);
            QLockOn.SetSkillshot(0.3f, 60f, 1800f, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.2658f, 120f, 1500f, false, SkillshotType.SkillshotCircle);
            
            Config = new Menu("Urgot", "Urgot", true);
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
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            
            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseEFarm", "Use E").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("QFarmType", "Q").SetValue(new StringList(new[] { "Any", "Any (E buff)", "Siege", "Siege (E buff)" })));
            Config.SubMenu("Farm").AddItem(new MenuItem("FarmMana", "Farm Mana >").SetValue(new Slider(50)));

            Config.AddSubMenu(new Menu("Jungle Farm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(true));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("UseWShield", "Auto W").SetValue(false));
            //Config.SubMenu("Misc").AddItem(new MenuItem("", "Auto R").SetValue(true));
            //Config.SubMenu("Misc").AddItem(new MenuItem("", "W after R").SetValue(true));

            Config.AddToMainMenu();

            //AntiGapcloser.OnEnemyGapcloser -= OnEnemyGapcloser;
            MYOrbwalker.BeforeAttack += BeforeAttack;
            //MYOrbwalker.OnAttack += OnAttack;
            GameObject.OnCreate += OnCreate;
            GameObject.OnDelete += OnDelete;
            Obj_AI_Base.OnProcessSpellCast += Game_OnProcessSpell;
            Game.OnUpdate += OnUpdate;
            
        }
        private static void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            
        }
        private static void BeforeAttack(MYOrbwalker.BeforeAttackEventArgs args)
        {
            if (args.Target is Obj_AI_Hero)
            {
                if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Combo)
                {
                    if (W.IsReady() && Config.Item("UseWCombo").GetValue<bool>() && Orbwalking.InAutoAttackRange(args.Target))
                    {
                        W.Cast();
                    }
                }
            }
        }
       
        private static void OnCreate(GameObject obj, EventArgs args)
        {
            if (obj is Obj_SpellMissile && obj.IsValid)
            {
                var missile = (Obj_SpellMissile) obj;                
                if (missile.SpellCaster.IsMe)
                {
                    if (obj.Type == GameObjectType.obj_SpellCircleMissile || obj.Type == GameObjectType.obj_SpellLineMissile)                    
                    {
                        if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Combo &&
                            ItemData.Muramana.GetItem().IsReady() &&
                            !ObjectManager.Player.HasBuff("Muramana") && GetPlayerManaPercentage() > 33)
                        {
                            Items.UseItem(3042);                            
                        }
                    }                             
                }
            }
        }

        private static void OnDelete(GameObject obj, EventArgs args)
        {
            if (obj is Obj_SpellMissile && obj.IsValid)
            {
                var missile = (Obj_SpellMissile) obj;
                if (missile.SpellCaster.IsMe)
                {
                    if (obj.Type == GameObjectType.obj_SpellCircleMissile || obj.Type == GameObjectType.obj_SpellLineMissile)                       
                    {
                        if (GetPlayerManaPercentage() < 33 || (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.None && 
                            ObjectManager.Player.HasBuff("Muramana")))
                        {
                            Items.UseItem(3042);
                        }
                    }       
                }
            }
        }

        private static void Game_OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs spell)
        {
            if (!unit.IsMe && unit is Obj_AI_Hero && unit.IsEnemy && spell.Target.IsMe && Config.Item("UseWShield").GetValue<bool>())
            {
                W.Cast();
            }
        }
        private static void OnUpdate(EventArgs args)
        {
            if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.None)
            {
                LockedTargetSelector.UnlockTarget();                
            }
            if (ObjectManager.Player.IsDead) return;
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active) Combo();
            if (Config.Item("HarassActive").GetValue<KeyBind>().Active) Harass();
            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active) LaneClear();
            if (Config.Item("JungleClearActive").GetValue<KeyBind>().Active) JungleClear();
        }

        private static void Combo()
        {
            var target = LockedTargetSelector.GetTarget(QLockOn.Range, TargetSelector.DamageType.Physical);
            if (target.IsValidTarget())
            {
                if (Config.Item("UseQCombo").GetValue<bool>() && Q.IsReady())
                {
                    if (target.HasBuff("urgotcorrosivedebuff"))
                    {
                        QLockOn.Cast(target.ServerPosition);
                    }
                    else
                    {
                        try
                        {
                            PredictionOutput pred = Q.GetPrediction(target);
                            if (pred.Hitchance == HitChance.VeryHigh && pred.CollisionObjects.Count(x => x.IsEnemy && !x.IsDead) < 1)
                            {
                                Q.Cast(ObjectManager.Player.ServerPosition.Extend(target.ServerPosition, QLockOn.Range));
                            }
                        }
                        catch { }
                    }
                }

                if (Config.Item("UseECombo").GetValue<bool>() && E.IsReady())
                {
                    EPredict(target);
                }
            }
        }
        private static void Harass()
        {            
            var target = TargetSelector.GetTarget(QLockOn.Range, TargetSelector.DamageType.Physical);
            if (target.IsValidTarget()) 
            {
                if (Config.Item("UseQHarass").GetValue<bool>() && Q.IsReady())
                {
                    if (target.HasBuff("urgotcorrosivedebuff"))
                    {
                        QLockOn.Cast(target.ServerPosition);
                    }
                    else
                    {
                        if (Q.IsInRange(target)) Q.Cast(target.ServerPosition);
                    }
                }
                if (Config.Item("UseEHarass").GetValue<bool>() && E.IsReady() && E.IsInRange(target))
                {
                    EPredict(target);
                }
            }
        }
        private static void LaneClear()
        {
            if (GetPlayerManaPercentage() < Config.Item("FarmMana").GetValue<Slider>().Value) return;
            if (Config.Item("UseQFarm").GetValue<bool>() && Q.IsReady() && !ObjectManager.Player.IsWindingUp && !MYOrbwalker.IsWaiting())               
            {                
                switch (Config.Item("QFarmType").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        var AnyMinionQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).Where(x => Q.IsKillable(x)).OrderBy(i => i.Distance(ObjectManager.Player)).FirstOrDefault();
                        Q.CastIfHitchanceEquals(AnyMinionQ, HitChance.High);
                        break;
                    case 1:
                        var minionQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, QLockOn.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).Where(x => MinionBuffCheck(x) && Q.IsKillable(x)).OrderByDescending(i => i.Distance(ObjectManager.Player)).FirstOrDefault();
                        QLockOn.Cast(minionQ.ServerPosition);
                        break;
                    case 2:
                        var siegeQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).FirstOrDefault(x => x.BaseSkinName.Contains("Siege") && Q.IsKillable(x));
                        Q.CastIfHitchanceEquals(siegeQ, HitChance.High);
                        break;
                    case 3:
                        var buffedsiegeQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, QLockOn.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).FirstOrDefault(x => x.BaseSkinName.Contains("Siege") && MinionBuffCheck(x) && Q.IsKillable(x));
                        QLockOn.Cast(buffedsiegeQ.ServerPosition);
                        break;
                }
            }
            if (Config.Item("UseEFarm").GetValue<bool>() && E.IsReady() && !MYOrbwalker.IsWaiting())
            {
                var siegeE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).FirstOrDefault(x => x.BaseSkinName.Contains("Siege"));
                if (siegeE != null)
                {
                    E.Cast(siegeE.ServerPosition);
                }
                else
                {
                    List<Obj_AI_Base> MinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range + E.Width, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health);
                    MinionManager.FarmLocation ECircular = E.GetCircularFarmLocation(MinionsE);
                    if (ECircular.MinionsHit >= 3)
                    {
                        E.Cast(ECircular.Position.To3D());
                    }
                }
            }
        }

        private static void JungleClear()
        {
            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            var largemobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.Health).FirstOrDefault(x => !x.BaseSkinName.Contains("Mini"));
            if (mobs.Count <= 0) return;
            var mob = mobs[0];
            if (mob == null) return;
            if (Config.Item("UseQJFarm").GetValue<bool>() && Q.IsReady())
            {
                if (largemobs != null)
                {
                    if (MinionBuffCheck(largemobs)) QLockOn.Cast(largemobs.ServerPosition);
                    else Q.Cast(largemobs.ServerPosition);
                }
                else
                {
                    if (MinionBuffCheck(mob)) QLockOn.Cast(mob.ServerPosition);
                    else Q.Cast(mob.ServerPosition);
                }
            }
            if (Config.Item("UseEJFarm").GetValue<bool>() && E.IsReady())
            {
                if (largemobs != null)
                {
                    E.Cast(largemobs.ServerPosition);
                }
                else
                {
                    E.Cast(mob.ServerPosition);
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
        private static bool MinionBuffCheck(Obj_AI_Base minion)
        {
            var buffs = minion.Buffs;
            foreach (var buff in buffs)
            {
                if (buff.Name == "urgotcorrosivedebuff")
                    return true;
            }
            return false;
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
                    if (prediction.Hitchance == HitChance.High && (Vector3.Distance(ObjectManager.Player.ServerPosition, enemy.ServerPosition) < E.Range))
                    {
                        closeToPrediction.Add(enemy);
                    }
                }
                if (closeToPrediction.Count == 0)
                {
                    E.CastIfHitchanceEquals(target, HitChance.VeryHigh);
                }
                else if (closeToPrediction.Count > 0)
                {
                    E.CastIfWillHit(target, closeToPrediction.Count, false);
                }
            }
        }
       
    }
}
/* Q is single target if enemy have E debuff
 * E = 150 radius, 900 range
 * W is Sheild
 * R is Suppress
 *  target.HasBuff("urgotcorrosivedebuff")
 * 
 * UrgotTerrorCapacitor2
 * urgotswap
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using myOrbwalker;
using LeagueSharp.Common;
using LeagueSharp;
using SharpDX;

namespace Leona
{
    class Program
    {
        private const string ChampionName = "Leona";
        private static Menu Config;
        private static Spell Q, W, E, R, RSharp;
        private static Obj_AI_Hero lastTarget;      
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName != ChampionName) return;

            Q = new Spell(SpellSlot.Q, ObjectManager.Player.AttackRange);
            W = new Spell(SpellSlot.W, ObjectManager.Player.AttackRange);
            E = new Spell(SpellSlot.E, 800f);
            R = new Spell(SpellSlot.R, 1200f);
            RSharp = new Spell(SpellSlot.R, 1200f);

            E.SetSkillshot(0.25f, 120f, 2000f, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(1f, 250f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            RSharp.SetSkillshot(1f, 125f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            Config = new Menu("Leona", "Leona", true);
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
            Config.SubMenu("Combo").AddItem(new MenuItem("EType", "E -").SetValue(new StringList(new[] { "Initiate", "Immobile" })));
            Config.SubMenu("Combo").AddItem(new MenuItem("RType", "R -").SetValue(new StringList(new[] { "Always", "Immobile", "Multi", "Killable" })));
            Config.SubMenu("Combo").AddItem(new MenuItem("Sticky", "Stick to Target").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseItemCombo", "Use Items").SetValue(true));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            
            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseEFarm", "Use E").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("FarmMana", "Farm Mana >").SetValue(new Slider(50)));

            Config.AddSubMenu(new Menu("Jungle Farm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));

            Config.AddSubMenu(new Menu("Misc Manager", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("AutoShield", "Auto Shield").SetValue(false));
            Config.SubMenu("Misc").AddItem(new MenuItem("QGap", "Anti Gap Closers").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("InterruptSpells", "Interrupt Spells").SetValue(false));
            Config.SubMenu("Misc").AddItem(new MenuItem("EQInterrupt", "Use EQ on Target").SetValue(false));
            Config.SubMenu("Misc").AddItem(new MenuItem("RInterrupt", "Use R on Target").SetValue(false));
            

            Config.AddToMainMenu();

            Obj_AI_Base.OnProcessSpellCast += Game_OnProcessSpell;
            Game.OnUpdate += Game_OnUpdate;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            AntiGapcloser.OnEnemyGapcloser -= OnEnemyGapcloser;
            MYOrbwalker.AfterAttack += OnAfterAttack;
            MYOrbwalker.OnNonKillableMinion += OnNonKillableMinion;
        }
        private static void OnNonKillableMinion(AttackableUnit minion)
        {
            if (Config.Item("UseQFarm").GetValue<bool>())
            {
                var target = minion as Obj_AI_Minion;
                if (target != null && Q.IsKillable(target) && Q.IsReady() && QBuff())
                {
                    Q.Cast(target);
                }
            }
        }
        private static void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Config.Item("QGap").GetValue<bool>())
            {
                if (Orbwalking.InAutoAttackRange(gapcloser.Sender))
                {
                    if (Q.IsReady())
                    {
                        Q.Cast();
                        ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, gapcloser.Sender);
                    }
                }
            }
        }
        private static void Game_OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs spell)
        {
            if (unit.IsMe)
            {
                //if ((spell.SData.Name.ToLower() == "leonashieldofdaybreak") || (spell.SData.Name.ToLower() == "leonashieldofdaybreakattack"))
                //{
                //    MYOrbwalker.ResetAutoAttackTimer();
                //}
            }
            if (!unit.IsMe)
            {
                /*
                if (!unit.IsValid<Obj_AI_Hero>())
                {
                    return;
                }
                if (!unit.IsEnemy)
                {
                    return;
                }
                if (spell.Target == null || !spell.Target.IsValid || !spell.Target.IsMe)
                {
                    return;
                }
                if (spell.SData.IsAutoAttack())
                {
                    return;
                }
                if (Config.Item("AutoShield").GetValue<bool>())
                {
                    Utility.DelayAction.Add(250, () => W.Cast());
                }*/
            }
            
        }
        private static void OnAfterAttack(AttackableUnit unit, AttackableUnit target)
        {

            if (!unit.IsMe) return;
            if (unit.IsMe)
            {/*
                if (!(target is Obj_AI_Hero))
                {
                    return;
                }
                if (!target.Name.ToLower().Contains("ward"))
                {
                    return;
                }
                if (!Q.IsReady())
                {
                    return;
                }
                if (Q.Cast())
                {
                    MYOrbwalker.ResetAutoAttackTimer();
                    ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                }*/
            }
        }
        private static void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (Config.Item("InterruptSpells").GetValue<bool>())
            {
                if (Orbwalking.InAutoAttackRange(sender) && Q.IsReady())
                {
                    Q.Cast();
                    ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, sender);
                }

                if (Vector3.Distance(ObjectManager.Player.ServerPosition, sender.ServerPosition) < E.Range && E.IsReady() && Q.IsReady())
                {
                    if (Config.Item("EQInterrupt").GetValue<bool>())
                    {
                        Q.Cast();
                        E.CastIfHitchanceEquals(sender, HitChance.High, false);
                        ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, sender);
                    }
                }
                else if (Vector3.Distance(ObjectManager.Player.ServerPosition, sender.ServerPosition) > E.Range)
                {
                    if (Config.Item("RInterrupt").GetValue<bool>())
                    {
                        if (Vector3.Distance(ObjectManager.Player.ServerPosition, sender.ServerPosition) < R.Range && R.IsReady())
                            R.CastIfHitchanceEquals(sender, HitChance.VeryHigh, false);
                    }
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
            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active) LaneClear();  
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active) Combo();
            if (Config.Item("HarassActive").GetValue<KeyBind>().Active) Harass();    
            if (Config.Item("JungleActive").GetValue<KeyBind>().Active) JungleClear();
           
        }
        private static void LaneClear()
        {
            if (GetPlayerManaPercentage() < Config.Item("FarmMana").GetValue<Slider>().Value) return;               
            if (Config.Item("UseQFarm").GetValue<bool>() && Q.IsReady())
            {                

            }
            if (Config.Item("UseEFarm").GetValue<bool>() && E.IsReady())
            {
                var minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range);
                MinionManager.FarmLocation pokehere = E.GetLineFarmLocation(minions);
                if (pokehere.Position.IsValid() && pokehere.MinionsHit >= 3)
                {
                    E.Cast(pokehere.Position);
                }
            }
        }
        private static void JungleClear()
        {
            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            if (mobs.Count <= 0) return;
            var mob = mobs[0];
            if (mob == null) return;
            if (Config.Item("UseQJFarm").GetValue<bool>() && Q.IsReady())
            {
                Q.Cast();
                ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, mob);
            }
        }
        private static void Combo()
        {
            var UseQ = Config.Item("UseQCombo").GetValue<bool>();
            var UseW = Config.Item("UseWCombo").GetValue<bool>();
            var UseE = Config.Item("UseECombo").GetValue<bool>();
            var UseR = Config.Item("UseRCombo").GetValue<bool>();
            var target = LockedTargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            var Sticks = Config.Item("Sticky").GetValue<bool>();
            var CastItems = Config.Item("UseItemCombo").GetValue<bool>();
            if (Sticks)
            {
                if (target.IsValidTarget()) SetOrbwalkingToTarget(target);
            }
            if (target.IsValidTarget()) 
            {
                if (target.InFountain()) return;
                if (target.UnderTurret(true) && !Config.Item("TurretDive").GetValue<bool>()) return;
                if (target.UnderTurret(true) && Config.Item("TurretDive").GetValue<bool>() && GetPlayerHealthPercentage() < 51) return;
                if (CastItems) { UseItems(0, target); }
                try
                {
                   
                    if (UseE && E.IsReady())
                    {
                        switch (Config.Item("EType").GetValue<StringList>().SelectedIndex)
                        {
                            case 0:
                                if (!Orbwalking.InAutoAttackRange(target) && Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) < E.Range)
                                {
                                    E.CastIfHitchanceEquals(target, HitChance.High);
                                }
                                break;
                            case 1:
                                foreach (var buff in target.Buffs)
                                {
                                    if (Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) < E.Range &&
                                        ((buff.Type == BuffType.Stun || buff.Type == BuffType.Taunt ||
                                          buff.Type == BuffType.Charm || buff.Type == BuffType.Fear ||
                                          buff.Type == BuffType.Suppression)))
                                    {
                                        E.CastIfHitchanceEquals(target, HitChance.High);
                                    }
                                }
                                break;
                        }
                    }

                    if (UseQ && Q.IsReady())
                    {
                        if (Orbwalking.InAutoAttackRange(target))
                        {
                            Q.Cast();
                            ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                            UseItems(1, target);
                            UseItems(2, target);
                        }
                    }

                    if (UseW && W.IsReady())
                    {
                        if (Orbwalking.InAutoAttackRange(target))
                        {
                            W.Cast();
                        }
                    }

                    if (UseR && R.IsReady() && Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) < R.Range)
                    {
                        switch (Config.Item("RType").GetValue<StringList>().SelectedIndex)
                        {
                            case 0:
                                R.CastIfHitchanceEquals(target, HitChance.VeryHigh);
                                break;
                            case 1:
                                foreach (var buff in target.Buffs)
                                {
                                    if ((buff.Type == BuffType.Stun || buff.Type == BuffType.Taunt ||
                                         buff.Type == BuffType.Charm || buff.Type == BuffType.Fear ||
                                         buff.Type == BuffType.Suppression))
                                    {
                                        RSharp.CastIfHitchanceEquals(target, HitChance.VeryHigh);
                                    }
                                }
                                break;
                            case 2:
                                RPredict(target);
                                break;
                            case 3:
                                if (R.IsKillable(target) && !Q.IsReady() && !W.IsReady() && !E.IsReady())
                                {
                                    RSharp.CastIfHitchanceEquals(target, HitChance.VeryHigh);
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
            var UseQ = Config.Item("UseQHarass").GetValue<bool>();
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (target != null)
            {
                if (UseQ && Q.IsReady())
                {
                    if (Orbwalking.InAutoAttackRange(target))
                    {
                        Q.Cast();
                        ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                    }
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
                if (Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) < 125f)
                {
                    MYOrbwalker.SetMovement(false);
                    if (target.IsMoving) MYOrbwalker.SetOrbwalkingPoint(ObjectManager.Player.ServerPosition.Shorten(target.ServerPosition, 20f));
                }
                else SetOrbwalkToDefault();
            }
            else SetOrbwalkToDefault();
        }
        private static float GetComboDamage(Obj_AI_Base target)
        {
            var damage = 0d;
            if (Config.Item("UseQCombo").GetValue<bool>() && Q.IsReady())
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q);
            if (Config.Item("UseWCombo").GetValue<bool>() && W.IsReady())
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.W);
            if (Config.Item("UseECombo").GetValue<bool>() && E.IsReady())
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.E);
            if (Config.Item("UseRCombo").GetValue<bool>() && R.IsReady())
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.R);
            return (float)damage;
        }
        private static void RPredict(Obj_AI_Base target)
        {
            PredictionOutput prediction;
            Vector3 pos1;
            prediction = R.GetPrediction(target);
            var nearChamps = (from champ in ObjectManager.Get<Obj_AI_Hero>() where champ.IsValidTarget(R.Range) && target != champ select champ).ToList();
            if (prediction.CastPosition.Distance(ObjectManager.Player.Position) < R.Range)
            {
                pos1 = prediction.CastPosition;
            }
            if (nearChamps.Count > 0)
            {
                var closeToPrediction = new List<Obj_AI_Hero>();
                foreach (var enemy in nearChamps)
                {
                    prediction = R.GetPrediction(enemy);
                    if (prediction.Hitchance == HitChance.High && Vector3.Distance(ObjectManager.Player.ServerPosition, enemy.ServerPosition) < R.Range)
                    {
                        closeToPrediction.Add(enemy);
                    }
                }
                if (closeToPrediction.Count > 0)
                {
                    //R.Cast(prediction.CastPosition);
                    RSharp.CastIfWillHit(target, closeToPrediction.Count, false);
                 
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
        private static bool QBuff()
        {
            return ObjectManager.Player.HasBuff("VolibearQ");
        }
    }
}

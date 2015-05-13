using System;
using System.Collections.Generic;
using System.Linq;
using myOrbwalker;
using LeagueSharp.Common;
using LeagueSharp;
using SharpDX;
using Color = System.Drawing.Color;

namespace Khazix
{
    class Program
    {
        private const string ChampionName = "Khazix";
        private static Menu Config;
        private static Spell Q, W, E, R;
        private static Obj_AI_Hero lastTarget, LockedTarget;
        private static List<Spell> Spells = new List<Spell>();
        private static Obj_AI_Hero Assasinate;
        private static Vector3 JumpStart, JumpEnd;
        private static int? JumpTime;
        private static bool JumpBool;
        private static int LastCast;        
        private static bool EvolvedQ;
        private static bool EvolvedW;
        private static bool EvolvedE;
        private static List<Obj_AI_Hero> EnemyList;
        private static bool Airborne;
        private static bool Landed = true;
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName != ChampionName) return;
            Q = new Spell(SpellSlot.Q, 325f);
            W = new Spell(SpellSlot.W, 1000f);
            E = new Spell(SpellSlot.E, 600f);
            R = new Spell(SpellSlot.R, 1200f);
            Spells.Add(Q);
            Spells.Add(W);
            Spells.Add(E);
            Spells.Add(R);
            W.SetSkillshot(0.225f, 100f, 828.5f, true, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.25f, 100f, 1000f, false, SkillshotType.SkillshotCircle);
            Config = new Menu("Khazix", "Khazix", true);
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
            Config.SubMenu("Combo").AddItem(new MenuItem("TurretDive", "Turret Dive").SetValue(false));
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboJumpDelay", "Delay ").SetValue(new Slider(1000, 1000, 5000)));
            Config.SubMenu("Combo").AddItem(new MenuItem("Sticky", "Stick to Target").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("StickyType", "Type").SetValue(new StringList(new[] { "AA Range", "Slider" })));
            Config.SubMenu("Combo").AddItem(new MenuItem("StickyTypeSlider", "Range").SetValue(new Slider(50, 50, 1000)));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseItemCombo", "Use Items").SetValue(true));
            
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(true));
            
            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseWFarm", "Use W").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("FarmMana", "Farm Mana >").SetValue(new Slider(50)));

            Config.AddSubMenu(new Menu("Jungle Farm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseWJFarm", "Use W").SetValue(true));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("DoubleJumpKey", "Double Jump").SetValue(new KeyBind(Config.Item("CustomMode_Key").GetValue<KeyBind>().Key, KeyBindType.Press)));  //T
            Config.SubMenu("Misc").AddItem(new MenuItem("DoubleJumpDirection", "").SetValue(new StringList(new[] { "Start Pos", "Cursor Pos" })));
            Config.SubMenu("Misc").AddItem(new MenuItem("DoubleJumpDraw", "Draw").SetValue(true));

            Config.AddToMainMenu();
            EnemyList = ObjectManager.Get<Obj_AI_Hero>().ToList();

            Obj_AI_Base.OnProcessSpellCast += Game_OnProcessSpell;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
            MYOrbwalker.AfterAttack += OnAfterAttack;
            
            GameObject.OnCreate += OnCreate;
            Game.OnUpdate += Game_OnUpdate;
            CustomEvents.Unit.OnDash += OnDash;
            Drawing.OnEndScene += Drawing_OnEndScene;
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
        private static void Game_OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs spell)
        { 
            if (unit.IsMe)
            {
                if (spell.SData.Name.ToLower() == "khazixq")
                {
                    MYOrbwalker.ResetAutoAttackTimer();
                }
                if (spell.SData.Name.ToLower() == "khazixqlong")
                {
                    MYOrbwalker.ResetAutoAttackTimer();
                }
            }
        }        
        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs spell)
        {
            if (sender == null || !sender.Owner.IsMe)
            {
                return;
            }
            if (spell.Slot == SpellSlot.E && MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Combo)
            {
                if (Environment.TickCount - LastCast < Config.Item("ComboJumpDelay").GetValue<Slider>().Value)
                {
                    spell.Process = false;
                    return;
                }
                LastCast = Environment.TickCount;
            }
        }        
        private static void OnAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe) return;
            if (unit.IsMe)
            {                
                if (!Q.IsReady())
                {
                    return;
                }
                
                if (Q.Cast())
                {
                    if (target is Obj_AI_Hero && !ObjectManager.Player.IsWindingUp && Landed)
                    {
                        if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Combo || MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Harass)
                        {
                            MYOrbwalker.ResetAutoAttackTimer();
                            if (Orbwalking.InAutoAttackRange(target)) ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                        }
                    }
                    if (target is Obj_AI_Minion && target.Team == GameObjectTeam.Neutral)
                    {
                        MYOrbwalker.ResetAutoAttackTimer();
                        if (Orbwalking.InAutoAttackRange(target)) ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                    }
                }                                
            }
        }
        private static void OnCreate(GameObject obj, EventArgs args)
        {
            if (obj != null && obj.IsValid && obj.Name == "Khazix_Base_E_WeaponTrails.troy")
            {
                Landed = false;

            }
            if (obj != null && obj.IsValid && obj.Name == "Khazix_Base_E_Land.troy")
            {
                Landed = true;
            }
        }
        private static void Drawing_OnEndScene(EventArgs args)
        {
            if (ObjectManager.Player.IsDead) return;
            var target = MYOrbwalker.ComboLocked ? LockedTargetSelector._lastTarget : MYOrbwalker.GetEnemyChampion();
            if (target != null) Render.Circle.DrawCircle(target.Position, 125, Color.Lime, 7, true);

            if (Config.Item("DoubleJumpDraw").GetValue<bool>() && EvolvedE)
            {
                var EnemyList = HeroManager.AllHeroes.Where(x => x.IsValidTarget() && x.IsEnemy && !x.IsDead && !x.IsZombie && !x.IsInvulnerable);
                var QKillableList = EnemyList.Where(x => !x.InFountain() && x.IsVisible && !Orbwalking.InAutoAttackRange(x) && GetQDamage(x) > x.Health);
                if (MYOrbwalker.CurrentMode != MYOrbwalker.Mode.Combo && Q.IsReady() && E.IsReady())
                {
                    foreach (var starget in QKillableList)
                    {
                        if (Vector3.Distance(ObjectManager.Player.ServerPosition, starget.ServerPosition) < E.Range)
                        {
                            Render.Circle.DrawCircle(starget.Position, 125, Color.Red, 7, true);
                        }
                        else Utility.DrawCircle(starget.Position, E.Range, Color.Red, 1, 30, true); 
                    }
                }
            }
        }
        private static void Game_OnUpdate(EventArgs args)
        {            
            EvolvedSpell();
            JumpStatus();
            if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.None)
            {
                LockedTargetSelector.UnlockTarget();
                MYOrbwalker.UnlockTarget();
                SetOrbwalkToDefault();
                Assasinate = null;
            }  
            if (ObjectManager.Player.IsDead) return;            
            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active) LaneClear();
            if (Config.Item("JungleActive").GetValue<KeyBind>().Active) JungleClear();
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active) Combo();
            if (Config.Item("HarassActive").GetValue<KeyBind>().Active) Harass();                 
            if (Config.Item("DoubleJumpKey").GetValue<KeyBind>().Active) DoubleJump();
                      
        }

        private static void LaneClear()
        {
            if (GetPlayerManaPercentage() < Config.Item("FarmMana").GetValue<Slider>().Value) return;
            if (Q.IsReady())
            {
                Obj_AI_Base tempTarget = null;
                float[] maxhealth;
                maxhealth = new float[] { 0 };
                var maxhealth2 = maxhealth;
                foreach (var minion in ObjectManager.Get<Obj_AI_Minion>().Where(
                    minion =>
                        minion.IsValidTarget(Q.Range) && minion.Team != GameObjectTeam.Neutral).Where(
                        minion => minion.MaxHealth > maxhealth2[0] || Math.Abs(maxhealth2[0] - float.MaxValue) < float.Epsilon))
                {
                    tempTarget = minion;
                    maxhealth[0] = minion.MaxHealth;
                }
                if (tempTarget != null)
                {
                    if (Airborne)
                    {
                        Q.CastOnUnit(tempTarget);
                        UseItems(2, tempTarget);                        
                    }
                    else if (Landed && Config.Item("UseQFarm").GetValue<bool>() && !ObjectManager.Player.IsWindingUp && MYOrbwalker.IsWaiting() &&
                             (ObjectManager.Player.GetSpellDamage(tempTarget, SpellSlot.Q) > tempTarget.Health))
                    {
                        Q.CastOnUnit(tempTarget);
                    }
                }
            }
            if (Config.Item("UseWFarm").GetValue<bool>() && W.IsReady())
            {
                MinionManager.FarmLocation farmLocation = MinionManager.GetBestLineFarmLocation(MinionManager.GetMinions(ObjectManager.Player.Position, W.Range).Select(minion => minion.ServerPosition.To2D()).ToList(), W.Width, W.Range);
                if (ObjectManager.Player.Distance(farmLocation.Position) < W.Range && !ObjectManager.Player.IsWindingUp)
                {
                    W.Cast(farmLocation.Position);
                }
            }
        }
        private static void JungleClear()
        {
            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            var largemobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.Health).FirstOrDefault(x => !x.BaseSkinName.Contains("Mini"));
            if (mobs.Count <= 0) return;
            var mob = mobs[0];
            if (mob != null)
            {
                if ((mobs.Count > 0) && Orbwalking.InAutoAttackRange(mob))
                {
                    UseItems(2, mob);
                }
                if (Config.Item("UseQJFarm").GetValue<bool>() && Q.IsReady())
                {
                    if (largemobs != null)
                    {
                        if (Airborne)
                        {
                            Q.CastOnUnit(largemobs);
                        }
                        else if (Landed && Q.IsInRange(largemobs))
                        {
                            Q.CastOnUnit(largemobs);
                        }
                    }
                    else
                    {
                        if (Airborne)
                        {
                            Q.CastOnUnit(mob);
                        }
                        else if (Landed && Q.IsInRange(mob))
                        {
                            Q.CastOnUnit(mob);
                        }
                    }
                }
                if (Config.Item("UseWJFarm").GetValue<bool>() && W.IsReady())
                {
                    if (largemobs != null)
                    {
                        W.Cast(largemobs.ServerPosition);
                    }
                    W.Cast(mob.ServerPosition);
                }
            }
        }
        private static void Combo()
        {
            Obj_AI_Hero target;
            if (IsolatedList() != null && IsolatedList().Any())
            {
                var Picks = IsolatedList().Where(
                    enemy =>
                    enemy.Distance(ObjectManager.Player.Position) < (E.Range * 1.5f)
                    ).OrderBy(enemy => enemy.Health).FirstOrDefault();
                target = Picks;
                IsolatedList().Clear();
            }
            else
            {
                if (MYOrbwalker.ComboLocked)
                {
                    target = LockedTargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                }
                else
                {
                    target = MYOrbwalker.GetEnemyChampion();
                }
            }
            var UseQ = Config.Item("UseQCombo").GetValue<bool>();
            var UseW = Config.Item("UseWCombo").GetValue<bool>();
            var UseE = Config.Item("UseECombo").GetValue<bool>();
            var UseR = Config.Item("UseRCombo").GetValue<bool>();
            var Sticks = Config.Item("Sticky").GetValue<bool>();
            var UseItems = Config.Item("UseItemCombo").GetValue<bool>();
            if (Sticks)
            {
                if (target.IsValidTarget()) SetOrbwalkingToTarget(target);
            }
            if (target.IsValidTarget())
            {
                if (target.InFountain()) return;                
                DoCombo(target, UseQ, UseW, UseE, UseR, UseItems);                
            }
            else SetOrbwalkToDefault();
        }
        private static void Harass()
        {
            var UseQ = Config.Item("UseQHarass").GetValue<bool>();
            var UseW = Config.Item("UseWHarass").GetValue<bool>();
            if (UseQ && Q.IsReady())
            {
                var targetQ = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                if (Q.IsInRange(targetQ))
                {
                    if (targetQ.UnderTurret(true) && ObjectManager.Player.UnderTurret(true)) return;
                    Q.CastOnUnit(targetQ);
                    if (Orbwalking.InAutoAttackRange(targetQ)) ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, targetQ);
                }
            }
            if (UseW && W.IsReady())
            {
                var targetW = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
                if (Vector3.Distance(ObjectManager.Player.ServerPosition, targetW.ServerPosition) <= W.Range && !ObjectManager.Player.IsWindingUp)
                {
                    if (targetW.UnderTurret(true) && ObjectManager.Player.UnderTurret(true)) return;
                    W.Cast(targetW.ServerPosition);
                }      
            }
        }
        private static void DoubleJump()
        {
            if (!EvolvedE) return;            
            var EnemyList = HeroManager.AllHeroes.Where(x => x.IsValidTarget() && !x.IsInvulnerable && !x.IsZombie);
            var target = EnemyList.FirstOrDefault(x => Vector3.Distance(ObjectManager.Player.ServerPosition, x.ServerPosition) < E.Range && GetQDamage(x) > x.Health);
            if (Assasinate == null && target != null)
            {
                Assasinate = target;
                MYOrbwalker.SetForcedTarget(target);
                JumpStart = Vector3.Zero;
                JumpEnd = Vector3.Zero;
                JumpTime = null;
                JumpBool = false;
            }
            if (Assasinate != null)
            {
                if (!JumpBool)
                {
                    var pred = E.GetPrediction(Assasinate);
                    if (pred.Hitchance >= HitChance.High)
                    {
                        var newcas = ExtendedE(Assasinate.Position, 1000f);
                        if (Q.IsReady() && E.IsReady() && ObjectManager.Player.Mana >= 125)
                        {
                            E.Cast(newcas);                            
                            JumpBool = true;
                        }                        
                        else if (ObjectManager.Player.Mana < 125)
                        {                            
                            return;
                        }
                    }
                }
                if (JumpBool)
                {
                    try
                    {
                        if ((Environment.TickCount - JumpTime < (500 + Game.Ping)) && ObjectManager.Player.IsDashing())
                        {
                            Q.CastOnUnit(Assasinate);
                            switch (Config.Item("DoubleJumpDirection").GetValue<StringList>().SelectedIndex)
                            {
                                case 0:
                                    E.Cast(ExtendedE(JumpStart, 1000f));
                                    break;
                                case 1:
                                    E.Cast(ExtendedE(Game.CursorPos, 1000f));
                                    break;
                            }
                        }
                    }
                    catch { }
                }
            }
        }
        private static void DoCombo(Obj_AI_Hero target, bool q, bool w, bool e, bool r, bool item)
        {
            if (item && !Stealth())
            {
                UseItems(0, target);
            }
            if (r && R.IsReady()) RChase(target);
            try
            {
                if (e && E.IsReady() && !Orbwalking.InAutoAttackRange(target))
                {

                    PredictionOutput pred = E.GetPrediction(target);
                    if (pred.Hitchance >= HitChance.High)
                    {
                        var v1 = target.Distance(ObjectManager.Player);
                        var v2 = target.Distance(pred.CastPosition);
                        var newcas = ExtendedE(target.Position, Math.Abs(v1 + v2 / 2));
                        if (newcas.UnderTurret(true) && !Config.Item("TurretDive").GetValue<bool>()) return;
                        if (newcas.UnderTurret(true) && Config.Item("TurretDive").GetValue<bool>() && GetPlayerHealthPercentage() < 50 && !EvolvedE) return;
                        E.Cast(newcas);
                    }
                }
                if (q && Q.IsReady())
                {
                    if (Airborne)
                    {
                        Q.CastOnUnit(target);
                        UseItems(2, target);
                    }
                    else if (Landed && Q.IsInRange(target))
                    {
                        Q.CastOnUnit(target);
                        if (Orbwalking.InAutoAttackRange(target)) ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                    }
                }
                if (w && W.IsReady())
                {
                    if (EvolvedW)
                    {
                        if (Landed && !Q.IsReady() && !ObjectManager.Player.IsWindingUp)
                        {
                            PredictionOutput wpred = W.GetPrediction(target, true);
                            if (wpred.Hitchance == HitChance.VeryHigh) W.Cast(wpred.CastPosition);
                        }
                    }
                    else if (!EvolvedW)
                    {
                        if (Landed && !Q.IsReady() && !ObjectManager.Player.IsWindingUp)
                        {
                            W.Cast(target);
                        }
                    }

                }
                if (item && Orbwalking.InAutoAttackRange(target) && !ObjectManager.Player.IsWindingUp)
                {
                    UseItems(1, target);
                    UseItems(2, target);                    
                }
            }
            catch { }
        }
        private static void RChase(Obj_AI_Base target)
        {
            if (!R.IsReady() && !target.IsValidTarget()) return;
            try
            {
                var dist = ObjectManager.Player.Position.To2D().Distance(target.Position.To2D());
                var msDif = ObjectManager.Player.MoveSpeed - target.MoveSpeed;
                if (msDif <= 0 && R.IsReady())
                {
                    R.Cast();
                }
                var reachIn = dist / msDif;
                if (reachIn > 1 && R.IsReady() && !Stealth())
                {
                    R.Cast();
                }
            }
            catch { }
        }

        private static List<Obj_AI_Hero> IsolatedList()
        {
            var AllEnemies = EnemyList.Where(enemy => enemy.IsEnemy && Geometry.Distance(ObjectManager.Player, enemy) < E.Range);
            var marked = new List<Obj_AI_Hero>();
            foreach (var Available in AllEnemies)
            {
                var IsolatedTargets = ObjectManager.Get<Obj_AI_Base>().Where(
                    pick => pick.IsEnemy &&
                        Available.NetworkId != pick.NetworkId &&
                        Available.ServerPosition.Distance(pick.ServerPosition) < 500).ToList();
                if (!IsolatedTargets.Any())
                {
                    if (!Available.IsDead && Available.IsVisible)
                    {
                        marked.Add(Available);
                    }
                }
            }
            return marked;
        }

        private static float GetPlayerHealthPercentage()
        {
            return ObjectManager.Player.Health * 100 / ObjectManager.Player.MaxHealth;
        }
        private static float GetPlayerManaPercentage()
        {
            return ObjectManager.Player.Mana * 100 / ObjectManager.Player.MaxMana;
        }
        private static double GetQDamage(Obj_AI_Hero target)
        {
            var damage = 0d;
            if (EvolvedQ)
            {
                if (Isolated(target))
                {
                    damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q, 3);
                }
                else damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q, 2);
            }
            else
            {
                if (Isolated(target))
                {
                    damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q, 1);
                }
                else damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q, 0);
            }
            return damage;
        }
        private static double GetComboDamage(Obj_AI_Hero target)
        {
            var damage = 0d;
            if (Passive())
            {
                var dmglist = new List<int> { 15, 20, 25, 35, 45, 55, 65, 75, 85, 95, 110, 125, 140, 150, 160, 170, 180, 190 };
                var dmg = dmglist[ObjectManager.Player.Level - 1] + (ObjectManager.Player.BaseAbilityDamage * 0.5) + (ObjectManager.Player.BaseAttackDamage);
                damage += dmg;
            }
            else
            {
                damage += ObjectManager.Player.BaseAttackDamage;
            }
            if (Config.Item("UseQCombo").GetValue<bool>() && Q.IsReady())
            {
                damage += GetQDamage(target);
            }
            if (Config.Item("UseWCombo").GetValue<bool>() && W.IsReady())
            {
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.W);
            }
            if (Config.Item("UseECombo").GetValue<bool>() && E.IsReady())
            {
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.E);
            }
            return (float)damage;
        }        
        
        private static bool Passive()
        {
            return ObjectManager.Player.HasBuff("khazixpdamage");
        }
        private static bool Stealth()
        {
            return ObjectManager.Player.HasBuff("khazixrstealth");
        }
        private static bool AnotherR()
        {
            return ObjectManager.Player.HasBuff("KhazixR");
        }
        private static bool Killable(Obj_AI_Hero target)
        {
            return target.Health < GetComboDamage(target);
        }
        private static bool Isolated(Obj_AI_Base target)
        {
            var Selects = ObjectManager.Get<Obj_AI_Base>()
            .Where(enemy =>
                enemy.IsEnemy &&
                enemy.NetworkId != target.NetworkId &&
                Vector3.Distance(target.ServerPosition, enemy.ServerPosition) < 500 &&
                !enemy.IsMe).ToArray();
            return !Selects.Any();
        }

        private static void EvolvedSpell()
        {
            if (ObjectManager.Player.HasBuff("khazixqevo", true))
            {
                EvolvedQ = true;
                Q.Range = 375;
            }
            if (ObjectManager.Player.HasBuff("khazixwevo", true))
            {
                EvolvedW = true;
                W.SetSkillshot(0.225f, 100f, 828.5f, true, SkillshotType.SkillshotCone);
            }
            if (ObjectManager.Player.HasBuff("khazixeevo", true))
            {
                EvolvedE = true;
                E.Range = 1000;
            }
        }
        private static void JumpStatus()
        {
            if (ObjectManager.Player.IsDashing()) Airborne = true;
            else Airborne = false;
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
            if (target != lastTarget)
            {
                SetOrbwalkToDefault();
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
        
        private static Vector3 ExtendedE(Vector3 posTarget, float modifier)
        {
            var newRange = ObjectManager.Player.ServerPosition.Extend(new Vector3(posTarget.X, posTarget.Y, posTarget.Z), modifier);
            return newRange;
        }
        private static void WPred(Obj_AI_Hero target)
        {
            PredictionOutput pred = W.GetPrediction(target);
            if (pred.Hitchance >= HitChance.High &&
                pred.CollisionObjects.Count(x => x.IsEnemy && !x.IsDead) < 1 &&
                Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) < W.Range)
            {
                W.Cast(
                    new Vector3(
                        target.ServerPosition.X + ((pred.UnitPosition.X - target.ServerPosition.X) / 2),
                        target.ServerPosition.Y + ((pred.UnitPosition.Y - target.ServerPosition.Y) / 2),
                        target.ServerPosition.Z));
            }
        }
    }
}
 
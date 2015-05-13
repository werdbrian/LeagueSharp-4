using System;
using System.Collections.Generic;
using System.Linq;
using myOrbwalker;
using LeagueSharp.Common;
using LeagueSharp;
using LeagueSharp.Common.Data;
using SharpDX;
using Color = System.Drawing.Color;


namespace Aatrox
{
    class Program
    {
        private const string ChampionName = "Aatrox";
        private static Menu Config;
        private static Spell Q, W, E, R, QSharp;
        private static Obj_AI_Hero lastTarget;        
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName != ChampionName) return;
            Q = new Spell(SpellSlot.Q, 650f);
            QSharp = new Spell(SpellSlot.Q, 650f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 1000f, TargetSelector.DamageType.Magical);
            R = new Spell(SpellSlot.R, 550, TargetSelector.DamageType.Magical);

            Q.SetSkillshot(0, 250, 2000, false, SkillshotType.SkillshotCircle);
            QSharp.SetSkillshot(0, 150, 2000, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.235f, 40, 1250, false, SkillshotType.SkillshotLine);
            
            Config = new Menu("Aatrox", "Aatrox", true);
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
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(false));
            Config.SubMenu("Combo").AddItem(new MenuItem("TurretDive", "Turret Dive").SetValue(false));
            Config.SubMenu("Combo").AddItem(new MenuItem("MustHavePassive", "Have Passive (Turret Dive)").SetValue(false));
            Config.SubMenu("Combo").AddItem(new MenuItem("Sticky", "Stick to Target").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("StickyType", "Type").SetValue(new StringList(new[] { "AA Range", "Slider" })));
            Config.SubMenu("Combo").AddItem(new MenuItem("StickyTypeSlider", "Range").SetValue(new Slider(50, 50, 1000)));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseItemCombo", "Use Items").SetValue(true));
            
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            
            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseEFarm", "Use E").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("QFarmValue", "Q More Than").SetValue(new Slider(1, 1, 5)));
            Config.SubMenu("Farm").AddItem(new MenuItem("EFarmValue", "E More Than").SetValue(new Slider(1, 1, 5)));

            Config.AddSubMenu(new Menu("Jungle Farm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(true));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("MiscW", "Auto W").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("MiscWPower", "Power when % >").SetValue(new Slider(50)));
            Config.SubMenu("Misc").AddItem(new MenuItem("MiscWLife", "Life when % <").SetValue(new Slider(40)));

            Config.AddToMainMenu();
            MYOrbwalker.AfterAttack += OnAfterAttack;
            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnEndScene += Drawing_OnEndScene;

        }
        private static void Drawing_OnEndScene(EventArgs args)
        {
            if (ObjectManager.Player.IsDead || ObjectManager.Player.IsZombie) return;
            var target = MYOrbwalker.ComboLocked ? LockedTargetSelector._lastTarget : MYOrbwalker.GetEnemyChampion();
            if (target != null) Render.Circle.DrawCircle(target.Position, 125, Color.Lime, 7, true);
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
            if (ObjectManager.Player.IsDead || ObjectManager.Player.IsZombie) return;
            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active) LaneClear();
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active) Combo();
            if (Config.Item("HarassActive").GetValue<KeyBind>().Active) Harass(); 
            if (Config.Item("JungleActive").GetValue<KeyBind>().Active) JungleClear();
            SmartW();            
        }
        private static void LaneClear()
        {
            if (Config.Item("UseQFarm").GetValue<bool>() && Q.IsReady())
            {
                var MinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range + Q.Width, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health);
                MinionManager.FarmLocation QCircular = Q.GetCircularFarmLocation(MinionsQ);
                if (QCircular.MinionsHit > Config.Item("QFarmValue").GetValue<Slider>().Value && !ObjectManager.Player.IsWindingUp && !MYOrbwalker.IsWaiting())
                {
                    if (QCircular.Position.To3D().Extend(ObjectManager.Player.ServerPosition, 20f).UnderTurret(true)) return;
                    if (Vector3.Distance(QCircular.Position.To3D(), ObjectManager.Player.ServerPosition) < ObjectManager.Player.AttackRange) return;
                    Q.Cast(QCircular.Position.To3D().Extend(ObjectManager.Player.ServerPosition, 20f));                    
                }
            }
            if (Config.Item("UseEFarm").GetValue<bool>() && E.IsReady())
            {
                var MinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health);
                MinionManager.FarmLocation ELine = E.GetLineFarmLocation(MinionsE);
                if (ELine.MinionsHit > Config.Item("EFarmValue").GetValue<Slider>().Value && !ObjectManager.Player.IsWindingUp && !MYOrbwalker.IsWaiting())
                {
                    if (ObjectManager.Player.UnderTurret(true)) return;
                    E.Cast(ELine.Position);
                }
            }
        }
        private static void JungleClear()
        {
            var largemobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.Health).FirstOrDefault(x => !x.BaseSkinName.Contains("Mini"));            
            if (Config.Item("UseQJFarm").GetValue<bool>() && Q.IsReady() && !ObjectManager.Player.IsWindingUp)
            {
                if (largemobs != null)
                {
                    Q.Cast(largemobs.ServerPosition.Extend(ObjectManager.Player.ServerPosition, 40f));
                }
                var MobsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range + Q.Width, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                MinionManager.FarmLocation QCircular = Q.GetCircularFarmLocation(MobsQ);
                if (QCircular.MinionsHit > 0)
                {
                    Q.Cast(QCircular.Position.To3D().Extend(ObjectManager.Player.ServerPosition, 50f));
                }
            }
            if (Config.Item("UseEJFarm").GetValue<bool>() && E.IsReady() && !ObjectManager.Player.IsWindingUp)
            {
                if (largemobs != null)
                {
                    E.Cast(largemobs.ServerPosition);
                }
                var MobsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                MinionManager.FarmLocation ELine = E.GetLineFarmLocation(MobsE);
                if (ELine.MinionsHit > 0)
                {
                    E.Cast(ELine.Position);
                }
            }
        }
        private static void Combo()
        {
            Obj_AI_Hero target;
            if (MYOrbwalker.ComboLocked)
            {
                target = LockedTargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
            }
            else
            {
                target = MYOrbwalker.GetEnemyChampion();
            }
            var UseQ = Config.Item("UseQCombo").GetValue<bool>();
            var UseE = Config.Item("UseECombo").GetValue<bool>();
            var UseR = Config.Item("UseRCombo").GetValue<bool>();
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
                    if (UseQ && Q.IsReady() && Q.IsInRange(target) && !ObjectManager.Player.IsWindingUp)
                    {
                        QPredict(target);
                    }
                    if (UseE && E.IsReady() && !ObjectManager.Player.IsWindingUp)
                    {
                        E.CastIfHitchanceEquals(target, HitChance.High);
                    }
                    if (UseR && R.IsReady() && !Q.IsReady() && !ObjectManager.Player.IsWindingUp)
                    {
                        R.Cast();
                    }
                    if (Orbwalking.InAutoAttackRange(target) && CastItems && !ObjectManager.Player.IsWindingUp)
                    {
                        UseItems(1, target);
                    }
                }
                catch { }
            }
            else SetOrbwalkToDefault();
        }
        private static void Harass()
        {
            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            if (target != null)
            {
                if (ObjectManager.Player.UnderTurret(true) && target.UnderTurret(true)) return;
                if (Config.Item("UseEHarass").GetValue<bool>() && E.IsReady() && E.IsInRange(target) && !ObjectManager.Player.IsWindingUp)
                {
                    E.CastIfHitchanceEquals(target, HitChance.High);
                }
            }
        }
        private static float GetPlayerHealthPercentage()
        {
            return ObjectManager.Player.Health * 100 / ObjectManager.Player.MaxHealth;
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
            return ObjectManager.Player.HasBuff("aatroxpassiveready");
        }
        private static float GetComboDamage(Obj_AI_Base target)
        {
            return 0f;
        }        
        private static void SmartW()
        {
            if (ObjectManager.Player.InFountain() || ObjectManager.Player.InShop() || ObjectManager.Player.HasBuff("Recall")) return;
            if (Config.Item("MiscW").GetValue<bool>())
            {
                if (GetPlayerHealthPercentage() > Config.Item("MiscWPower").GetValue<Slider>().Value)
                {
                    if (ObjectManager.Player.HasBuff("aatroxwlife")) W.Cast();
                    return;
                }
                if (GetPlayerHealthPercentage() < Config.Item("MiscWLife").GetValue<Slider>().Value)
                {
                    if (ObjectManager.Player.HasBuff("aatroxwpower")) W.Cast();
                    return;
                }
            }
        }
        private static bool Killable(Obj_AI_Base target)
        {
            return target.Health < GetComboDamage(target);
        }        
        private static void QPredict(Obj_AI_Base target)
        {           
            var nearChamps = (from champ in ObjectManager.Get<Obj_AI_Hero>() where champ.IsValidTarget(Q.Range) && target != champ select champ).ToList();
            if (nearChamps.Count > 0)
            {
                var closeToPrediction = new List<Obj_AI_Hero>();
                foreach (var enemy in nearChamps)
                {
                    PredictionOutput prediction = Q.GetPrediction(enemy);
                    if (prediction.Hitchance >= HitChance.High && Vector3.Distance(target.ServerPosition, enemy.ServerPosition) < 100f)
                    {
                        closeToPrediction.Add(enemy);
                    }
                }
                if (target.UnderTurret(true) && Config.Item("TurretDive").GetValue<bool>())
                {
                     if (Config.Item("MustHavePassive").GetValue<bool>() && !Passive()) return;
                }
                if (closeToPrediction.Count == 0)
                {
                    PredictionOutput pred = Q.GetPrediction(target);
                    if (pred.Hitchance >= HitChance.High && Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) < Q.Range)
                    {
                        Q.Cast(pred.CastPosition.Extend(ObjectManager.Player.ServerPosition, -30f));
                    }
                }
                else if (closeToPrediction.Count > 0)
                {
                    Q.CastIfWillHit(target, closeToPrediction.Count);
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
    }
}
using System;
using System.Linq;
using LeagueSharp.Common;
using LeagueSharp;
using LeagueSharp.Common.Data;
using myOrbwalker;
using SharpDX;
using Color = System.Drawing.Color;

namespace Vi
{
    class Program
    {
        private const string ChampionName = "Vi";
        private static Menu Config;
        private static Spell Q,Q2, E, E2, R;
        private static Obj_AI_Hero lastTarget;              
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName != ChampionName) return;
            Q = new Spell(SpellSlot.Q, 860f);
            Q2 = new Spell(SpellSlot.Q, 860f);
            E = new Spell(SpellSlot.E);
            E2 = new Spell(SpellSlot.E, 600f);
            R = new Spell(SpellSlot.R, 800f);

            Q.SetSkillshot(Q.Instance.SData.SpellCastTime, Q.Instance.SData.LineWidth, Q.Instance.SData.MissileSpeed, true, SkillshotType.SkillshotLine);
            Q2.SetSkillshot(Q.Instance.SData.SpellCastTime, Q.Instance.SData.LineWidth, Q.Instance.SData.MissileSpeed, false, SkillshotType.SkillshotLine);
            Q.SetCharged("ViQ", "ViQ", 100, 860, 1f);
            Q2.SetCharged("ViQ", "ViQ", 100, 860, 1f);
            E.SetSkillshot(0.15f, 150f, float.MaxValue, false, SkillshotType.SkillshotCone);
            R.SetTargetted(0.15f, 1500f);

            Config = new Menu("Vi", "Vi", true);
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
            Config.SubMenu("Combo").AddItem(new MenuItem("QMaxCombo", "Q Max Range").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("CRType", "R").SetValue(new StringList(new[] { "Killable", "CC - Initiate", "CC - Follow Up", "Furthest" })));
            Config.SubMenu("Combo").AddItem(new MenuItem("TurretDive", "Turret Dive").SetValue(false));
            Config.SubMenu("Combo").AddItem(new MenuItem("Sticky", "Stick to Target").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("StickyType", "Type").SetValue(new StringList(new[] { "AA Range", "Slider" })));
            Config.SubMenu("Combo").AddItem(new MenuItem("StickyTypeSlider", "Range").SetValue(new Slider(50, 50, 1000)));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseItemCombo", "Use Items").SetValue(true));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            
            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseEFarm", "Use E").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("QFarmType", "Q").SetValue(new StringList(new[] { "Any", "Furthest" })));
            Config.SubMenu("Farm").AddItem(new MenuItem("QFarmValue", "(Any) Q More Than").SetValue(new Slider(1, 1, 5)));
            Config.SubMenu("Farm").AddItem(new MenuItem("FarmMana", "Farm Mana >").SetValue(new Slider(50)));

            Config.AddSubMenu(new Menu("Jungle Farm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(true));

            Config.AddToMainMenu();

            Obj_AI_Base.OnProcessSpellCast += Game_OnProcessSpell;
            Game.OnUpdate += OnUpdate;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            //AntiGapcloser.OnEnemyGapcloser -= OnEnemyGapcloser;
            MYOrbwalker.AfterAttack += OnAfterAttack;
            Drawing.OnEndScene += Drawing_OnEndScene;
        }
        private static void Drawing_OnEndScene(EventArgs args)
        {
            if (ObjectManager.Player.IsDead) return;
            var target = MYOrbwalker.ComboLocked ? LockedTargetSelector._lastTarget : MYOrbwalker.GetEnemyChampion();
            if (target != null) Render.Circle.DrawCircle(target.Position, 125, Color.Lime, 7, true);
        }
        private static void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {

        }
        private static void Game_OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs spell)
        {
            if (unit.IsMe && spell.SData.Name.ToLower() == "vie")
            {
                MYOrbwalker.ResetAutoAttackTimer();
            }            
        }
        private static void OnAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe) return;
            if (unit.IsMe)
            {
                if (target is Obj_AI_Hero && !ObjectManager.Player.IsWindingUp)
                {
                    if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Combo)
                    {
                        if (E.IsReady() && !EBuff() &&
                            Config.Item("UseECombo").GetValue<bool>() &&
                            Orbwalking.InAutoAttackRange(target))
                        {
                            E.Cast();
                            ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                        }
                        if (HaveItems() && Config.Item("UseItemCombo").GetValue<bool>() && Orbwalking.InAutoAttackRange(target))
                        {
                            UseItems(2, null);
                        }
                    }
                    if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Harass)
                    {
                        if (E.IsReady() && !EBuff() &&
                            Config.Item("UseEHarass").GetValue<bool>() &&
                            Orbwalking.InAutoAttackRange(target))
                        {
                            E.Cast();
                            ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                        }
                    }
                }
                if (target is Obj_AI_Minion && !ObjectManager.Player.IsWindingUp && E.IsReady() && !EBuff() && !ObjectManager.Player.IsWindingUp && Orbwalking.InAutoAttackRange(target))
                {
                    if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.LaneClear && GetPlayerManaPercentage() > Config.Item("FarmMana").GetValue<Slider>().Value)
                    {
                        if (Config.Item("UseEFarm").GetValue<bool>() && E.IsKillable((Obj_AI_Minion)target))
                        {
                            E.Cast();
                            ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                        }
                    }
                    if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.JungleClear)
                    {
                        UseItems(2, null);
                        if (Config.Item("UseEJFarm").GetValue<bool>())
                        {
                            E.Cast();
                            ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                        }
                    }
                }
            }
        }
        private static void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {

        }
        private static void OnUpdate(EventArgs args)
        {
            if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.None)
            {
                LockedTargetSelector.UnlockTarget();
                SetOrbwalkToDefault();
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
            if (Config.Item("UseQFarm").GetValue<bool>() && Q.IsReady())
            {
                var AllMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Enemy);
                switch (Config.Item("QFarmType").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        MinionManager.FarmLocation QLine = Q2.GetLineFarmLocation(AllMinionsQ);
                        if (QLine.Position.IsValid() && !QLine.Position.To3D().UnderTurret(true))
                        {
                            if (QLine.MinionsHit > Config.Item("QFarmValue").GetValue<Slider>().Value)
                            {
                                if (Q2.IsCharging)
                                {
                                    if (Q2.Range >= Q.ChargedMaxRange)
                                    {
                                        Q2.Cast(QLine.Position);
                                    }
                                }
                                else
                                {
                                    Q2.StartCharging();
                                }
                            }
                        }
                        break;
                    case 1:
                        var FurthestQ = AllMinionsQ.OrderByDescending(i => i.Distance(ObjectManager.Player)).FirstOrDefault(x => !x.UnderTurret(true));
                        if (FurthestQ != null && FurthestQ.Position.IsValid())
                        {
                            if (Q2.IsCharging)
                            {
                                if (Q2.Range >= Q.ChargedMaxRange)
                                {
                                    Q2.Cast(FurthestQ.Position);
                                }
                            }
                            else
                            {
                                Q2.StartCharging();
                            }
                            
                        }
                        break;
                }
            }
        }
        private static void JungleClear()
        {
            var largemobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range * 1.5f, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.Health).FirstOrDefault(x => !x.BaseSkinName.Contains("Mini"));
            if (largemobs != null && Config.Item("UseQJFarm").GetValue<bool>() && !Orbwalking.InAutoAttackRange(largemobs))
            {
                if (Q2.IsCharging)
                {
                    if (Q2.Range >= Q.ChargedMaxRange)
                    {                        
                        Q2.Cast(largemobs.ServerPosition);
                    }
                }
                else
                {
                    Q2.StartCharging();
                }
            }
        }
        private static void Combo()
        {
            Obj_AI_Hero target;
            if (MYOrbwalker.ComboLocked)
            {
                target = LockedTargetSelector.GetTarget(Q.Range * 1.5f, TargetSelector.DamageType.Physical);
            }
            else
            {
                target = MYOrbwalker.GetEnemyChampion();
            }
            var UseQ = Config.Item("UseQCombo").GetValue<bool>();
            var UseR = Config.Item("UseRCombo").GetValue<bool>();
            var RType = Config.Item("CRType").GetValue<StringList>().SelectedIndex;
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
                    if (UseQ && Q.IsReady())
                    {
                        if (target.UnderTurret(true) && !Config.Item("TurretDive").GetValue<bool>()) return;
                        if (Q.IsCharging)
                        {                           
                            if (Config.Item("QMaxCombo").GetValue<bool>())
                            {
                                if (Q.Range >= Q.ChargedMaxRange) 
                                {                            
                                    Q.Cast(ObjectManager.Player.ServerPosition.Extend(target.ServerPosition, 125f));
                                }
                            }
                            else if (!Config.Item("QMaxCombo").GetValue<bool>())
                            {
                                if (Q.Range >= Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition))
                                {
    
                                    Q.Cast(ObjectManager.Player.ServerPosition.Extend(target.ServerPosition, 125f));
                                }
                            }
                            else if (target.UnderTurret(true) && !Config.Item("TurretDive").GetValue<bool>())
                            {
                                Q.Cast(ObjectManager.Player.ServerPosition.Extend(Game.CursorPos, 125f));
                            }
                        }
                        else
                        {
                            Q.StartCharging();
                        }
                    }

                    if (UseR && R.IsReady() && 
                       Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) <= R.Range &&
                        !ObjectManager.Player.IsWindingUp)
                    {
                        if (target.UnderTurret(true) && !Config.Item("TurretDive").GetValue<bool>()) return;
                        switch (RType)
                        {
                            case 0:
                                if (R.IsKillable(target) && !Q.IsReady() && !E.IsReady() && E.Instance.Ammo < 1)
                                {
                                    R.Cast(target);
                                }
                                break;
                            case 1:
                                R.Cast(target);
                                break;
                            case 2:
                                foreach (var buff in target.Buffs)
                                {
                                    if ((buff.Type == BuffType.Knockback || buff.Type == BuffType.Knockup ||
                                         buff.Type == BuffType.Snare || buff.Type == BuffType.Stun ||
                                         buff.Type == BuffType.Taunt || buff.Type == BuffType.Charm ||
                                         buff.Type == BuffType.Fear || buff.Type == BuffType.Suppression))
                                    {
                                        R.Cast(target);
                                    }
                                }
                                break;
                            case 3:
                                var EnemyList = HeroManager.AllHeroes.Where(x => x.IsValidTarget() && x.IsEnemy && !x.IsDead && !x.IsZombie && !x.IsInvulnerable);
                                var targetR = EnemyList.Where(x => Vector3.Distance(ObjectManager.Player.ServerPosition, x.ServerPosition) < R.Range).OrderByDescending(i => i.Distance(ObjectManager.Player)).FirstOrDefault();                  
                                if (targetR.IsValidTarget())
                                {
                                    if (targetR.UnderTurret(true) && !Config.Item("TurretDive").GetValue<bool>()) return;
                                    R.Cast(targetR);
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
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (target.IsValidTarget() &&  !ObjectManager.Player.UnderTurret(true))
            {
                if (UseQ && Q.IsReady())
                {
                    if (Q.IsCharging)
                    {
                        if (Q.Range >= Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition))
                        {
                            if (target.ServerPosition.UnderTurret(true)) return;
                            Q.Cast(target.ServerPosition);
                        }
                    }
                    else
                    {
                        Q.StartCharging();
                    }
                }
            }
        }
        private static bool EBuff()
        {
            return ObjectManager.Player.HasBuff("ViE");
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
        private static bool HaveItems()
        {
            if (ItemData.Tiamat_Melee_Only.GetItem().IsReady() || ItemData.Ravenous_Hydra_Melee_Only.GetItem().IsReady()) return true;
            else return false;
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
using System;
using System.Linq;
using myOrbwalker;
using LeagueSharp.Common.Data;
using LeagueSharp.Common;
using LeagueSharp;
using SharpDX;
using Color = System.Drawing.Color;


namespace Fiora
{
    class Program
    {
        private const string ChampionName = "Fiora";
        private static Menu Config;
        private static Spell Q, W, E, R;
        private static Obj_AI_Hero lastTarget;
        private static int LastCast = Environment.TickCount;
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName != ChampionName) return;
            Q = new Spell(SpellSlot.Q, 600f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 400f);

            Config = new Menu("Fiora", "Fiora", true);
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
            Config.SubMenu("Combo").AddItem(new MenuItem("CRType", "R").SetValue(new StringList(new[] { "Killable", "Multi", "Always" })));
            Config.SubMenu("Combo").AddItem(new MenuItem("CRNum", "(Multi) R targets >").SetValue(new Slider(2, 2, 5)));
            Config.SubMenu("Combo").AddItem(new MenuItem("TurretDive", "Turret Dive").SetValue(false));
            Config.SubMenu("Combo").AddItem(new MenuItem("Sticky", "Stick to Target").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("StickyType", "Type").SetValue(new StringList(new[] { "AA Range", "Slider" })));
            Config.SubMenu("Combo").AddItem(new MenuItem("StickyTypeSlider", "Range").SetValue(new Slider(50, 50, 1000)));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseItemCombo", "Use Items").SetValue(true));
            
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            
            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(false));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseEFarm", "Use E").SetValue(false));
            Config.SubMenu("Farm").AddItem(new MenuItem("QFarmType", "Q").SetValue(new StringList(new[] { "Any", "Furthest - Closest", "Only Siege" })));
            Config.SubMenu("Farm").AddItem(new MenuItem("FarmMana", "Farm Mana >").SetValue(new Slider(50)));

            Config.AddSubMenu(new Menu("Jungle Farm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(true));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("UseWBlock", "Auto W").SetValue(false));

            Config.AddToMainMenu();
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
            Game.OnUpdate += Game_OnUpdate;
            MYOrbwalker.AfterAttack += OnAfterAttack;
            //MYOrbwalker.BeforeAttack += BeforeAttack;
            Spellbook.OnCastSpell += OnCastSpell;
            Drawing.OnEndScene += Drawing_OnEndScene;
        }
        private static void Drawing_OnEndScene(EventArgs args)
        {
            if (ObjectManager.Player.IsDead) return;
            var target = MYOrbwalker.ComboLocked ? LockedTargetSelector._lastTarget : MYOrbwalker.GetEnemyChampion();
            if (target != null) Render.Circle.DrawCircle(target.Position, 125, Color.Lime, 7, true);
        }
        private static void OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs spell)
        {
            if (!unit.IsMe && spell.Target.IsMe && unit.IsChampion(unit.BaseSkinName))
            {
                if (MYOrbwalker.CurrentMode != MYOrbwalker.Mode.Combo || MYOrbwalker.CurrentMode == MYOrbwalker.Mode.None)
                {
                    if (spell.SData.Name.Contains("Attack") && Config.Item("UseWBlock").GetValue<bool>())
                    {
                        Utility.DelayAction.Add(400, () =>  W.Cast());
                    }
                }
                if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Combo)
                {
                    if (spell.SData.Name.Contains("Attack") && !ObjectManager.Player.IsWindingUp && Config.Item("UseWCombo").GetValue<bool>())
                    {
                        W.Cast();
                    }
                }
            }
            if (unit.IsMe)
            {
                if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Combo || MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Harass)
                {
                    if (spell.SData.Name.ToLower() == "fioraq" && !ObjectManager.Player.IsWindingUp)
                    {
                        ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, spell.Target);
                    }
                    if (spell.SData.Name.ToLower() == "fioraflurry" && !ObjectManager.Player.IsWindingUp)
                    {
                        ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, spell.Target);                    
                    }
                }
            }
        }     
        private static void OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs spell)
        {
            if (sender == null || !sender.Owner.IsMe)
            {
                return;
            }
            if (spell.Slot == SpellSlot.Q)
            {
                LastCast = Environment.TickCount;
            }
        }

        private static void BeforeAttack(MYOrbwalker.BeforeAttackEventArgs args)
        {
            if (args.Target is Obj_AI_Minion && args.Target.Team == GameObjectTeam.Neutral)
            {
                if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.JungleClear &&
                    Config.Item("UseEJFarm").GetValue<bool>() && E.IsReady() && !EBuff() &&
                    !args.Target.Name.Contains("Mini") &&                    
                    !ObjectManager.Player.IsWindingUp &&
                    Orbwalking.InAutoAttackRange(args.Target))
                {
                    UseItems(2, null);   
                    E.Cast();                    
                }
            }
        }

        private static void OnAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe) return;
            if (unit.IsMe)
            {
                if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Combo)
                {
                    if (E.IsReady() && !EBuff() && !(Q.IsReady() && QBuff()) && !ObjectManager.Player.IsWindingUp && Config.Item("UseECombo").GetValue<bool>() && Orbwalking.InAutoAttackRange(target))
                    {
                        E.Cast();
                    }
                    if (!(Q.IsReady() && QBuff() && E.IsReady()) && !ObjectManager.Player.IsWindingUp && HaveItems() && Config.Item("UseItemCombo").GetValue<bool>() && Orbwalking.InAutoAttackRange(target))
                    {
                        UseItems(2, null);                        
                    }
                }
                if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.JungleClear)
                {
                    if (target is Obj_AI_Minion && target.Team == GameObjectTeam.Neutral && !target.Name.Contains("Mini") &&
                        Config.Item("UseEJFarm").GetValue<bool>() && E.IsReady() && !EBuff() && !ObjectManager.Player.IsWindingUp && Orbwalking.InAutoAttackRange(target))
                    {
                        UseItems(2, null);
                        E.Cast();    
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
            if (ObjectManager.Player.IsDead) return;
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active) Combo();
            if (Config.Item("HarassActive").GetValue<KeyBind>().Active) Harass();
            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active) LaneClear();                
            if (Config.Item("JungleActive").GetValue<KeyBind>().Active) JungleClear();
           
        }
        private static void Combo()
        {
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
            var UseR = Config.Item("UseRCombo").GetValue<bool>();
            var RType = Config.Item("CRType").GetValue<StringList>().SelectedIndex;
            var countrnum = Config.Item("CRNum").GetValue<Slider>().Value;
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
                        if (!QBuff() && !EBuff() && !ObjectManager.Player.IsWindingUp)
                        {
                            Q.Cast(target);
                            if (Orbwalking.InAutoAttackRange(target) && !ObjectManager.Player.IsWindingUp) ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                        }
                        else if (QBuff() && !ObjectManager.Player.IsWindingUp)
                        {
                            Q.Cast(target);
                            if (Orbwalking.InAutoAttackRange(target) && !ObjectManager.Player.IsWindingUp) ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                        }
                    }
                    if (UseR && R.IsReady() && R.IsInRange(target) && !QBuff() && !ObjectManager.Player.IsWindingUp)
                    {
                        switch (RType)
                        {
                            case 0:
                                if (R.IsKillable(target) && !E.IsReady()) R.Cast(target);
                                break;
                            case 1:
                                if (CountEnemies(target, R.Range) >= countrnum)
                                    R.Cast(target);
                                break;
                            case 2:
                                R.Cast(target);
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
            var target = LockedTargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (target.IsValidTarget() && UseQ && Q.IsReady() && Q.IsInRange(target))
            {
                if (!target.UnderTurret(true))
                {
                    if (!QBuff() && !ObjectManager.Player.IsWindingUp)
                    {
                        Q.Cast(target);
                        return;
                    }
                    if (QBuff() && Environment.TickCount - LastCast > 500 && !ObjectManager.Player.IsWindingUp)
                    {
                        Q.Cast(target);
                    }
                }
                else
                {
                    var ClosestQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).OrderBy(i => i.Distance(ObjectManager.Player)).FirstOrDefault(x => !x.UnderTurret(true));
                    if (!QBuff() && ClosestQ != null)
                    {
                        Q.Cast(target);
                    }
                    if (QBuff() && ClosestQ != null)
                    {
                        Q.Cast(ClosestQ);
                    }
                }
            }
        }
        private static void LaneClear()
        {
            if (GetPlayerManaPercentage() < Config.Item("FarmMana").GetValue<Slider>().Value) return;
            var minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, ObjectManager.Player.AttackRange*2, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.None);
            if (minions.Count >= 3 && !MYOrbwalker.IsWaiting() && !ObjectManager.Player.IsWindingUp)
            {
                UseItems(2, null);
            }
            if (Config.Item("UseQFarm").GetValue<bool>() && Q.IsReady())
            {
                var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health);
                switch (Config.Item("QFarmType").GetValue<StringList>().SelectedIndex)
                {
                    case 0:                        
                        foreach (var minionq in allMinionsQ)
                        {
                            if (minionq.Health < ObjectManager.Player.GetSpellDamage(minionq, SpellSlot.Q) && !ObjectManager.Player.IsWindingUp && !Orbwalking.InAutoAttackRange(minionq) && !minionq.UnderTurret(true))
                            {
                                if (!QBuff())
                                {
                                    Q.Cast(minionq);
                                }
                                if (QBuff() &&  Environment.TickCount - LastCast > 1000 )
                                {
                                    Q.Cast(minionq);
                                }
                            }
                        }
                        break;
                    case 1:
                        //var FurthestQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).OrderByDescending(i => i.Distance(ObjectManager.Player)).FirstOrDefault();
                        //var ClosestQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).OrderBy(i => i.Distance(ObjectManager.Player)).FirstOrDefault();
                        var FurthestQ = allMinionsQ.OrderByDescending(i => i.Distance(ObjectManager.Player)).First();
                        var ClosestQ = allMinionsQ.OrderBy(i => i.Distance(ObjectManager.Player)).First();
                        if (FurthestQ.IsValidTarget() && ClosestQ.IsValidTarget() && FurthestQ != ClosestQ)
                        {
                            Q.Cast(FurthestQ);
                            Utility.DelayAction.Add(500, () => Q.Cast(ClosestQ));
                        }
                        break;
                    case 2:
                        var siegeQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).FirstOrDefault(x => x.BaseSkinName.Contains("Siege") && Q.IsKillable(x));
                        Q.Cast(siegeQ);
                        break;
                }
            }
            if (Config.Item("UseEFarm").GetValue<bool>() && E.IsReady())
            {
                var allMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, ObjectManager.Player.AttackRange * 2, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.None).Count();
                if (allMinionsE >= 3 && !ObjectManager.Player.IsWindingUp)
                {
                    E.Cast();
                }
            }
        }
        private static void JungleClear()
        {
            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            var largemobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.Health).FirstOrDefault(x => !x.BaseSkinName.Contains("Mini"));
            if (mobs.Count <= 0) return;
            var mob = mobs[0];
            if (mob != null)
            {
                if (Config.Item("UseQJFarm").GetValue<bool>() && Q.IsReady())
                {
                    if (largemobs != null)
                    {
                        if (Q.IsKillable(largemobs))
                        {
                            Q.Cast(largemobs);
                        }
                        if (!QBuff())
                        {
                            Q.Cast(largemobs);
                        }
                        if (QBuff())
                        {
                            Q.Cast(largemobs);
                        }
                    }
                    else 
                    {
                        if (!QBuff())
                        {
                            Q.Cast(mob);
                        }
                        else if (QBuff() && Environment.TickCount - LastCast > 1000)
                        {
                            Q.Cast(mob);
                        }
                    }
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
            return ObjectManager.Player.HasBuff("FioraQCD");
        }
        private static bool EBuff()
        {
            return ObjectManager.Player.HasBuff("FioraFlurry");
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
        private static int CountEnemies(Obj_AI_Base target, float range)
        {
            return
            ObjectManager.Get<Obj_AI_Hero>()
            .Count(
            Enemy =>
            Enemy.IsValidTarget() && Enemy.Team != ObjectManager.Player.Team &&
            Enemy.ServerPosition.Distance(target.ServerPosition) <= range);
        }
    }
}
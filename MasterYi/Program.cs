using System;
using System.Linq;
using myOrbwalker;
using LeagueSharp.Common.Data;
using LeagueSharp.Common;
using LeagueSharp;
using SharpDX;
using Color = System.Drawing.Color;


namespace MasterYi
{
    class Program
    {
        private const string ChampionName = "MasterYi";
        private static Menu Config;
        private static Spell Q, W, E, R;
        private static Obj_AI_Hero lastTarget;
        private static Obj_AI_Hero target;
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName != ChampionName) return;
            Q = new Spell(SpellSlot.Q, 600);           
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R);

            Q.SetTargetted(0.5f, 2000);
            Config = new Menu("Master Yi", "MasterYi", true);
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
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQComboType", "Q").SetValue(new StringList(new[] { "Always", "Not AA Range", "Multi" })));
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
            Config.SubMenu("Farm").AddItem(new MenuItem("FarmQType", "Q").SetValue(new StringList(new[] { "Any", "Furthest" })));
            Config.SubMenu("Farm").AddItem(new MenuItem("FarmMana", "Farm Mana >").SetValue(new Slider(50)));

            Config.AddSubMenu(new Menu("Jungle Farm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(true));


            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("CircleTarget", "Circle Target").SetValue(true));

            Config.AddToMainMenu();
            Game.OnUpdate += Game_OnUpdate;
            MYOrbwalker.AfterAttack += OnAfterAttack;
            MYOrbwalker.BeforeAttack += BeforeAttack;
            Drawing.OnEndScene += OnEndScene;
        }
        private static void OnEndScene(EventArgs args)
        {
            if (ObjectManager.Player.IsDead) return;
            if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Combo && Config.Item("CircleTarget").GetValue<bool>())
            {                
                if (target != null) Render.Circle.DrawCircle(target.Position, 125, Color.Lime, 7, true);
            }
        }
        private static void BeforeAttack(MYOrbwalker.BeforeAttackEventArgs args)
        {
            if (args.Target is Obj_AI_Hero)
            {
                if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Combo)
                {
                    if (E.IsReady() && !ObjectManager.Player.IsWindingUp && Config.Item("UseECombo").GetValue<bool>() && Orbwalking.InAutoAttackRange(args.Target))
                    {
                        E.Cast();                        
                    }
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
                    if (!ObjectManager.Player.IsWindingUp && HaveItems() && Config.Item("UseItemCombo").GetValue<bool>() && Orbwalking.InAutoAttackRange(target))
                    {
                        UseItems(2, null);                       
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
            MYOrbwalker.SetAttack(!Channeling());
            MYOrbwalker.SetMovement(!Channeling());
            if (Channeling()) return;
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active) Combo();
            if (Config.Item("HarassActive").GetValue<KeyBind>().Active) Harass();
            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active) LaneClear();                
            if (Config.Item("JungleActive").GetValue<KeyBind>().Active) JungleClear();            
        }
        private static void Combo()
        {
            
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
                if (target.UnderTurret(true) && Config.Item("TurretDive").GetValue<bool>() && GetPlayerHealthPercentage() < 33) return;               
                if (CastItems) { UseItems(0, target); }
                try
                {                   
                    if (UseR && R.IsReady())
                    {
                        RChase(target);
                    }
                    if (UseQ && Q.IsReady() && !ObjectManager.Player.IsWindingUp) // && Q.IsInRange(target)  && !Orbwalking.InAutoAttackRange(target)
                    {
                        switch (Config.Item("UseQComboType").GetValue<StringList>().SelectedIndex)
                        {
                            case 0:
                               if (Q.IsInRange(target))
                               {
                                   Q.CastOnUnit(target);
                               }
                                break;
                            case 1:
                                if (Q.IsInRange(target)  && !Orbwalking.InAutoAttackRange(target))
                                {
                                    Q.CastOnUnit(target);
                                }
                                break;
                            case 2:
                                if (Q.IsInRange(target) && target.CountEnemiesInRange(Q.Range) > 0)
                                {
                                    Q.CastOnUnit(target);
                                }
                                break;
                        }
                        
                    }
                    if (Orbwalking.InAutoAttackRange(target) && CastItems)
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
            var UseQ = Config.Item("UseQHarass").GetValue<bool>();
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (target.IsValidTarget())
            {
                if (UseQ && Q.IsReady() &&  Q.IsInRange(target) && !ObjectManager.Player.IsWindingUp && !Orbwalking.InAutoAttackRange(target) && !target.UnderTurret(true))
                {
                    Q.CastOnUnit(target);
                }
            }
        }
        private static void LaneClear()
        {
            if (GetPlayerManaPercentage() < Config.Item("FarmMana").GetValue<Slider>().Value) return;
            var minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range);
            if (minions == null) return;
            if (Config.Item("UseQFarm").GetValue<bool>() && Q.IsReady() && !MYOrbwalker.IsWaiting() && !ObjectManager.Player.IsWindingUp)
            {
                switch (Config.Item("FarmQType").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        var AnyQ = minions.OrderByDescending(i => i.Distance(ObjectManager.Player)).FirstOrDefault(x => !x.UnderTurret(true));
                        {
                            Q.CastOnUnit(AnyQ);
                        }
                        break;
                    case 1:
                        //Furthest
                        var FurthestQ = minions.OrderByDescending(i => i.Distance(ObjectManager.Player)).Where(x => !x.UnderTurret(true)).ToList();
                        foreach (var x in FurthestQ) 
                        {
                            if (Q.IsInRange(x) && MinionManager.GetMinions(x.ServerPosition, 300).Count() > 2)
                            {
                                if (!Orbwalking.InAutoAttackRange(x)) Q.CastOnUnit(x);
                            }
                        }
                
                        break;
                }
            }          
        }
        private static void JungleClear()
        {
            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            if (mobs.Count <= 0) return;
            var mob = mobs[0];
            if (mob != null)
            {
                if ((mobs.Count > 0) && Orbwalking.InAutoAttackRange(mob))
                {
                    UseItems(2, mob);
                }
                if (Config.Item("UseQJFarm").GetValue<bool>() && Q.IsReady() && Q.IsInRange(mob))
                {
                    Q.CastOnUnit(mob);
                }
                if (Config.Item("UseEJFarm").GetValue<bool>() && E.IsReady() && Orbwalking.InAutoAttackRange(mob))
                {
                    E.Cast();
                }
            }
        }
        private static void UseItems(int type, Obj_AI_Base target)
        {
            Int16[] SelfItems = { 3142 }; //Just Ghostblade //3180, 3131, 3074, 3077, 
            Int16[] TargetingItems = { 3153, 3144 }; //Just botrk //3188, 3128, 3146, 3184 };
            Int16[] AoeItems = { 3074, 3077 }; //Just hydra //3180, 3131, 3074, 3077, 
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
        private static void RChase(Obj_AI_Hero target)
        {
            if (!R.IsReady() && !target.IsValidTarget()) return;
            try
            {
                var dist = ObjectManager.Player.Position.To2D().Distance(target.Position.To2D());
                var msDif = ObjectManager.Player.MoveSpeed - target.MoveSpeed;
                if (msDif <= 0 && R.IsReady())
                {
                    R.CastOnUnit(ObjectManager.Player);
                }
                var reachIn = dist / msDif;
                if (reachIn > 3 && R.IsReady())
                {
                    R.CastOnUnit(ObjectManager.Player);
                }
            }
            catch { }
        }
        private static bool Channeling()
        {
            return ObjectManager.Player.HasBuff("Meditate");
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
    }
}
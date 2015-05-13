using System;
using System.Collections.Generic;
using System.Linq;
using myOrbwalker;
using LeagueSharp.Common;
using LeagueSharp;
using SharpDX;

namespace Fiddlesticks
{
    class Program
    {
        private const string ChampionName = "FiddleSticks";
        private static Menu Config;
        private static Spell Q, W, E, R;
        private static bool ChannelingW;
        private static bool ChannelingR;
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName != ChampionName) return;

            Q = new Spell(SpellSlot.Q, 575);
            W = new Spell(SpellSlot.W, 575);
            E = new Spell(SpellSlot.E, 750);
            R = new Spell(SpellSlot.R, 800);

            Config = new Menu("Fiddlesticks", "Fiddlesticks", true);
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
            Config.SubMenu("Combo").AddItem(new MenuItem("TurretDive", "Turret Dive").SetValue(true));            

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            
            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseWFarm", "Use W").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseEFarm", "Use E").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("FarmMana", "Farm Mana >").SetValue(new Slider(50)));
            Config.SubMenu("Farm").AddItem(new MenuItem("FarmWType", "W").SetValue(new StringList(new[] { "Any", "Low HP", "Siege", "Low HP and Siege" })));
            Config.SubMenu("Farm").AddItem(new MenuItem("FarmWHP", "W if HP <").SetValue(new Slider(50)));

            Config.AddSubMenu(new Menu("Jungle Farm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseWJFarm", "Use W").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(true));

            Config.AddSubMenu(new Menu("Misc Manager", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("UseQGap", "Q Gapcloser").SetValue(false));

            Config.AddToMainMenu();

            Obj_AI_Base.OnProcessSpellCast += Game_OnProcessSpell;
            Game.OnUpdate += Game_OnUpdate;
            AntiGapcloser.OnEnemyGapcloser -= OnEnemyGapcloser;
            GameObject.OnCreate += OnCreate;
            Spellbook.OnCastSpell += OnCastSpell;
           
        }
        private static void OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (sender.Owner.IsMe)
            {
                if (WBuff() && (args.Slot == SpellSlot.Q || args.Slot == SpellSlot.W || args.Slot == SpellSlot.E))
                {
                    args.Process = false;
                }
            }
        }
        private static void OnCreate(GameObject obj, EventArgs args)
        {
            if (obj != null && obj.IsValid && obj.Name.Contains("Crowstorm") && obj.Name.Contains(".troy"))
            {
               // ChannelingR = false;
            }
        }
        private static void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Config.Item("UseQGap").GetValue<bool>())
            {
                if (ObjectManager.Player.Distance(gapcloser.Sender) <= Q.Range && Q.IsReady())
                {
                    Q.CastOnUnit(gapcloser.Sender);                    
                }
            }
        }
        private static void Game_OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs spell)
        {
            if (unit.IsMe)
            {
                if (spell.SData.Name.ToLower() == "drain")
                {                   
                  //  ChannelingW = true;
                }
                if (spell.SData.Name.ToLower() == "crowstorm")
                {
                   // ChannelingR = true;
                }
            }
        }
        private static void Game_OnUpdate(EventArgs args)
        {   
            if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.None)
            {
                LockedTargetSelector.UnlockTarget();
            }
            if (ObjectManager.Player.IsDead) return;
            MYOrbwalker.SetAttack(!Channeling());
            MYOrbwalker.SetMovement(!Channeling());
            if (Channeling()) return;
            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active) LaneClear();          
            if (Config.Item("JungleClearActive").GetValue<KeyBind>().Active) JungleClear();
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active) Combo();
            if (Config.Item("HarassActive").GetValue<KeyBind>().Active) Harass();
         
        }
        private static void LaneClear()
        {
             if (GetPlayerManaPercentage() < Config.Item("FarmMana").GetValue<Slider>().Value) return;
             if (Config.Item("UseWFarm").GetValue<bool>() && W.IsReady() && !ObjectManager.Player.IsWindingUp)
            {
                List<Obj_AI_Base> MinionsW = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);
                var cannonW = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).Where(x => x.BaseSkinName.Contains("Siege"));
                switch (Config.Item("FarmWType").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        //Always                        
                        W.CastOnUnit(MinionsW[0]);
                        break;                    
                    case 1:
                        //Low HP  && 
                        if (GetPlayerHealthPercentage() < Config.Item("FarmWHP").GetValue<Slider>().Value) W.CastOnUnit(MinionsW[0]);
                        break;
                    case 2:
                        //Siege                            
                        foreach (var siegeW in cannonW)
                        {
                            W.CastOnUnit(siegeW);
                        }
                        break;
                    case 3:
                        //Low HP and Siege                            
                        foreach (var siegeW in cannonW)
                        {
                            if (GetPlayerHealthPercentage() < Config.Item("FarmWHP").GetValue<Slider>().Value) W.CastOnUnit(siegeW);
                        }
                        break;
                }
            }
             if (Config.Item("UseEFarm").GetValue<bool>() && E.IsReady() && !Channeling() && !ObjectManager.Player.IsWindingUp)
            {
                List<Obj_AI_Base> MinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).OrderByDescending(i => i.Distance(ObjectManager.Player)).ToList();
                E.CastOnUnit(MinionsE[0]);
            }
        }
        private static void JungleClear()
        {
            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            if (mobs.Count <= 0) return;
            var mob = mobs[0];
            if (mob != null)
            {
                if (Config.Item("UseQJFarm").GetValue<bool>() && Q.IsReady() && Q.IsInRange(mob) && !Channeling() && !ObjectManager.Player.IsWindingUp)
                {
                    Q.CastOnUnit(mob);
                }
                if (Config.Item("UseWJFarm").GetValue<bool>() && W.IsReady() && W.IsInRange(mob) && !ObjectManager.Player.IsWindingUp)
                {
                    W.CastOnUnit(mob);
                }
                if (Config.Item("UseEJFarm").GetValue<bool>() && E.IsReady() && E.IsInRange(mob) && !Channeling() && !ObjectManager.Player.IsWindingUp)
                {
                    E.CastOnUnit(mob);
                }               
            }
        }
        private static void Combo()
        {
            var target = LockedTargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            var UseQ = Config.Item("UseQCombo").GetValue<bool>();
            var UseW = Config.Item("UseWCombo").GetValue<bool>();
            var UseE = Config.Item("UseECombo").GetValue<bool>();
            if (target.IsValidTarget())
            {
                if (UseQ && Q.IsReady() && Q.IsInRange(target) && !Channeling() && !ObjectManager.Player.IsWindingUp)
                {
                    Q.CastOnUnit(target);
                }
                if (UseW && W.IsReady() && W.IsInRange(target) && !ObjectManager.Player.IsWindingUp)
                {
                    W.CastOnUnit(target);
                }
                if (UseE && E.IsReady() && E.IsInRange(target) && !Channeling() && !ObjectManager.Player.IsWindingUp)
                {
                    E.CastOnUnit(target);
                }
            }
        }
        private static void Harass()
        {
            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            var UseQ = Config.Item("UseQHarass").GetValue<bool>();
            var UseW = Config.Item("UseWHarass").GetValue<bool>();
            var UseE = Config.Item("UseEHarass").GetValue<bool>();
            if (target.IsValidTarget())
            {
                if (ObjectManager.Player.UnderTurret(true) && target.UnderTurret(true)) return;
                if (UseQ && Q.IsReady() && Q.IsInRange(target) && !Channeling() && !ObjectManager.Player.IsWindingUp)
                {
                    Q.CastOnUnit(target);
                }
                if (UseW && W.IsReady() && W.IsInRange(target) && !ObjectManager.Player.IsWindingUp)
                {
                    W.CastOnUnit(target);
                }
                if (UseE && E.IsReady() && E.IsInRange(target) && 
                    (UseW && !W.IsReady()) &&
                    !Channeling() && !ObjectManager.Player.IsWindingUp)
                {
                    E.CastOnUnit(target);
                }
            }
        }
        private static float GetComboDamage(Obj_AI_Base target)
        {
            return 0f;
        }

        private static bool Channeling()
        {
            //return ChannelingR || ChannelingW || WBuff();
            return WBuff();
        }

        private static bool WBuff()
        {
            return ObjectManager.Player.HasBuff("Drain") || ObjectManager.Player.HasBuff("fearmonger_marker");
        }
        private static bool RBuff()
        {
            return ObjectManager.Player.HasBuff("Crowstorm");
        }
        private static float GetPlayerHealthPercentage()
        {
            return ObjectManager.Player.Health * 100 / ObjectManager.Player.MaxHealth;
        }
        private static float GetPlayerManaPercentage()
        {
            return ObjectManager.Player.Mana * 100 / ObjectManager.Player.MaxMana;
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

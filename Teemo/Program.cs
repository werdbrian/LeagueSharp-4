using System;
using System.Linq;
using myOrbwalker;
using LeagueSharp.Common;
using LeagueSharp;

namespace Teemo
{
    class Program
    {
        private const string ChampionName = "Teemo";        
        private static Menu Config;
        private static Spell Q, W, R;
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName != ChampionName) return;

            Q = new Spell(SpellSlot.Q, 580);
            W = new Spell(SpellSlot.W);
            R = new Spell(SpellSlot.R);

            Config = new Menu("Teemo", "Teemo", true);
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

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            
            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("QFarmType", "Q").SetValue(new StringList(new[] { "Any", "Siege" })));
            Config.SubMenu("Farm").AddItem(new MenuItem("FarmMana", "Farm Mana >").SetValue(new Slider(50)));

            Config.AddSubMenu(new Menu("Jungle Farm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));

            Config.AddToMainMenu();

            MYOrbwalker.AfterAttack += OnAfterAttack;
            MYOrbwalker.BeforeAttack += BeforeAttack;
            Game.OnUpdate += OnUpdate;
            MYOrbwalker.OnNonKillableMinion += OnNonKillableMinion;
        }
        private static void OnNonKillableMinion(AttackableUnit minion)
        {
            if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.LaneClear)
            {
                if (Config.Item("UseQFarm").GetValue<bool>() && GetPlayerManaPercentage() > Config.Item("FarmMana").GetValue<Slider>().Value && Config.Item("QFarmType").GetValue<StringList>().SelectedIndex == 0)
                {
                    var target = minion as Obj_AI_Minion;
                    if (target != null && Q.IsKillable(target) && Q.IsReady())
                    {
                        Q.Cast(target);
                    }
                }
            }
        }
        private static void BeforeAttack(MYOrbwalker.BeforeAttackEventArgs args)
        {
            if (args.Target is Obj_AI_Hero)
            {
                if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Combo)
                {
                    if (Q.IsKillable(args.Target) && Q.IsReady() && Config.Item("UseQCombo").GetValue<bool>() && !ObjectManager.Player.IsWindingUp)
                    {
                        Q.Cast(args.Target);
                    }
                }
            }
        }
        private static void OnAfterAttack(AttackableUnit unit, AttackableUnit target)
        {            
            if (!unit.IsMe) return;
            if (unit.IsMe)
            {
                if (target is Obj_AI_Hero)
                {
                    if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Combo && !ObjectManager.Player.IsWindingUp) 
                    {
                        if (Q.IsKillable((Obj_AI_Hero)target) && Q.IsReady() && Config.Item("UseQCombo").GetValue<bool>())
                        {
                            Q.Cast((Obj_AI_Hero)target);
                        }
                    }
                    if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.Harass && !ObjectManager.Player.IsWindingUp)
                    {
                        if (Q.IsReady() && Config.Item("UseQHarass").GetValue<bool>())
                        {
                            Q.Cast((Obj_AI_Hero)target);
                        }
                    }
                }
                if (target is Obj_AI_Minion)
                {
                    if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.JungleClear && target.Team == GameObjectTeam.Neutral)
                    {
                        if (Q.IsReady() && Config.Item("UseQJFarm").GetValue<bool>())
                        {
                            Q.Cast((Obj_AI_Minion)target);
                        }
                    }
                }
            }
        }
        private static void OnUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead) return;            
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active) Combo();
            if (Config.Item("HarassActive").GetValue<KeyBind>().Active) Harass();
            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active) LaneClear();
            if (MYOrbwalker.CurrentMode == MYOrbwalker.Mode.None)
            {
                LockedTargetSelector.UnlockTarget();
            }
        }
        private static void Combo()
        {
            var target = LockedTargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (target.IsValidTarget() && !ObjectManager.Player.IsWindingUp)
            {
                if (Config.Item("UseQCombo").GetValue<bool>() && Q.IsReady())
                {
                    Q.Cast(target);
                }
                if (Config.Item("UseWCombo").GetValue<bool>() && W.IsReady())
                {
                    WChase(target);
                }
            }
        }
        private static void Harass()
        {
            if (Config.Item("UseQHarass").GetValue<bool>() && Q.IsReady())
            {
                var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
                if (target.IsValidTarget() && !ObjectManager.Player.IsWindingUp)
                {
                   Q.Cast(target);
                }
            }
        }
        private static void LaneClear()
        {
            if (GetPlayerManaPercentage() < Config.Item("FarmMana").GetValue<Slider>().Value) return;
            if (Config.Item("UseQFarm").GetValue<bool>() && Q.IsReady() && !ObjectManager.Player.IsWindingUp && MYOrbwalker.IsWaiting())
            {
                switch (Config.Item("QFarmType").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        var AnyMinionQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).FirstOrDefault(x => Q.IsKillable(x));
                        Q.Cast(AnyMinionQ);
                        break;
                    case 1:
                        var siegeQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).FirstOrDefault(x => x.BaseSkinName.Contains("Siege") && Q.IsKillable(x));
                        Q.Cast(siegeQ);
                        break;
                }
            }
        }
        private static double GetAADamage()
        {
            if (ObjectManager.Player.HasBuff("Toxic Attack"))
            {
                return ((ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Level * 10 + ObjectManager.Player.FlatMagicDamageMod * 0.3) + ObjectManager.Player.BaseAttackDamage);
            }
            else return ObjectManager.Player.BaseAttackDamage;
        }
        private static void WChase(Obj_AI_Base target)
        {
            if (!W.IsReady() && !target.IsValidTarget()) return;
            try
            {
                var dist = ObjectManager.Player.Position.To2D().Distance(target.Position.To2D());
                var msDif = ObjectManager.Player.MoveSpeed - target.MoveSpeed;
                var reachIn = dist / msDif;
                if (reachIn > 3 && W.IsReady())
                {
                    W.Cast();
                    return;
                }
            }
            catch { }
        }
        private static float GetPlayerHealthPercentage()
        {
            return ObjectManager.Player.Health * 100 / ObjectManager.Player.MaxHealth;
        }
        private static float GetPlayerManaPercentage()
        {
            return ObjectManager.Player.Mana * 100 / ObjectManager.Player.MaxMana;
        }
    }
}

using BattleArena.Player;
using BattleArena.Progression;
using BattleArena.Spells;
using BattleArena.UI.Elements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BattleArena.UI
{
    /// <summary>
    /// "Your build" panel on the pause screen: equipped spells with their upgrades, and every stat.
    /// Elements are created once and refreshed each time the game is paused.
    /// </summary>
    internal sealed class BuildSummaryView
    {
        private static readonly string[] Numerals = { "I", "II", "III", "IV", "V", "VI" };
        private static readonly string[] SlotKeys = { "Q", "E", "R" };

        // Order in the two-column grid, so related stats share a row (Attack beside Attack speed). Independent of the
        // StatType values, which card assets store.
        private static readonly StatType[] StatOrder =
        {
            StatType.MoveSpeed, StatType.MaxHealth,
            StatType.Attack, StatType.AttackSpeed,
            StatType.CritChance, StatType.CritDamage,
            StatType.Area, StatType.Duration,
            StatType.Multishot, StatType.Pierce,
            StatType.PickupRadius, StatType.Armor,
            StatType.Regeneration, StatType.ExperienceGain,
            StatType.Luck, StatType.CooldownReduction,
        };

        private readonly SpellCaster _caster;
        private readonly PlayerStats _stats;
        private readonly LevelUpConfig _config;
        private readonly SpellRow[] _spellRows;
        private readonly Label[] _statValues;

        public BuildSummaryView(VisualElement root, SpellCaster caster, PlayerStats stats, LevelUpConfig config)
        {
            _caster = caster;
            _stats = stats;
            _config = config;

            var head = root.Q("build-scaling-head");
            for (var i = 0; i < SpellScalingOrder.Stats.Length; i++)
            {
                var column = new VisualElement { pickingMode = PickingMode.Ignore };
                column.AddToClassList("build-scaling-col");
                var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("icon");
                icon.AddToClassList(SpellScalingOrder.IconClasses[i]);
                icon.AddToClassList("build-scaling-col-icon");
                column.Add(icon);
                column.Add(CreateLabel(SpellScalingOrder.ShortNames[i], "build-scaling-col-label"));
                head.Add(column);
            }

            var spells = root.Q("build-spells");
            _spellRows = new SpellRow[caster.SlotCount];
            for (var i = 0; i < _spellRows.Length; i++)
            {
                _spellRows[i] = new SpellRow(i < SlotKeys.Length ? SlotKeys[i] : "?");
                spells.Add(_spellRows[i].Root);
            }

            var statsGrid = root.Q("build-stats");
            if (StatOrder.Length != PlayerStats.StatCount)
                Debug.LogWarning($"Build summary lists {StatOrder.Length} of {PlayerStats.StatCount} stats; add new stats to StatOrder.");
            _statValues = new Label[StatOrder.Length];
            for (var i = 0; i < _statValues.Length; i++)
            {
                var item = new VisualElement();
                item.AddToClassList("build-stat");
                item.Add(CreateLabel(PlayerStats.GetStatName(StatOrder[i]), "build-stat-label"));
                _statValues[i] = CreateLabel(string.Empty, "build-stat-value");
                item.Add(_statValues[i]);
                statsGrid.Add(item);
            }
        }

        public void Refresh()
        {
            for (var slot = 0; slot < _spellRows.Length; slot++)
            {
                var spell = _caster.GetSpell(slot);
                if (spell == null)
                    _spellRows[slot].ShowLocked(_caster.GetUnlockLevel(slot));
                else
                    _spellRows[slot].ShowSpell(spell, _caster.GetUpgrades(slot), _config);
            }

            for (var i = 0; i < _statValues.Length; i++)
                _statValues[i].text = _stats.FormatCurrent(StatOrder[i]);
        }

        private static Label CreateLabel(string text, string className)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(className);
            return label;
        }

        private sealed class SpellRow
        {
            private readonly VisualElement _icon;
            private readonly Label _name;
            private readonly Label _locked;
            private readonly VisualElement _chips;
            private readonly VisualElement _dots;

            public SpellRow(string key)
            {
                Root = new VisualElement { pickingMode = PickingMode.Ignore };
                Root.AddToClassList("build-spell");

                var frame = new VisualElement();
                frame.AddToClassList("build-spell-frame");
                var hex = new ShapeElement { Kind = ShapeKind.Hexagon };
                hex.AddToClassList("fill");
                hex.AddToClassList("build-spell-hex");
                frame.Add(hex);
                _icon = new VisualElement();
                _icon.AddToClassList("icon");
                _icon.AddToClassList("build-spell-icon");
                frame.Add(_icon);
                Root.Add(frame);

                var info = new VisualElement();
                info.AddToClassList("build-spell-info");
                _name = CreateLabel(string.Empty, "build-spell-name");
                _name.AddToClassList("display");
                _locked = CreateLabel(string.Empty, "build-spell-muted");
                _chips = new VisualElement();
                _chips.AddToClassList("build-spell-chips");
                info.Add(_name);
                info.Add(_locked);
                info.Add(_chips);
                Root.Add(info);

                // One dot per shared stat, lit in the spell's colour when it changes this spell.
                _dots = new VisualElement { pickingMode = PickingMode.Ignore };
                _dots.AddToClassList("build-spell-dots");
                for (var i = 0; i < SpellScalingOrder.Stats.Length; i++)
                {
                    var cell = new VisualElement { pickingMode = PickingMode.Ignore };
                    cell.AddToClassList("build-spell-dot-cell");
                    var dot = new VisualElement { pickingMode = PickingMode.Ignore };
                    dot.AddToClassList("build-spell-dot");
                    cell.Add(dot);
                    _dots.Add(cell);
                }
                Root.Add(_dots);

                var keycap = CreateLabel(key, "keycap");
                keycap.AddToClassList("keycap-small");
                Root.Add(keycap);
            }

            public VisualElement Root { get; }

            private void ShowScaling(SpellDefinition spell)
            {
                _dots.style.visibility = Visibility.Visible;
                for (var i = 0; i < SpellScalingOrder.Stats.Length; i++)
                {
                    var used = (spell.ScalesWith & SpellScalingOrder.Stats[i]) != 0;
                    var dot = _dots[i][0];
                    var color = used ? spell.Color : SpellScalingOrder.UnusedColor;
                    dot.style.backgroundColor = used ? spell.Color : Color.clear;
                    dot.style.borderTopColor = color;
                    dot.style.borderRightColor = color;
                    dot.style.borderBottomColor = color;
                    dot.style.borderLeftColor = color;
                }
            }

            public void ShowLocked(int unlockLevel)
            {
                Root.AddToClassList("locked");
                _icon.AddToClassList("icon-lock");
                _icon.style.backgroundImage = StyleKeyword.Null;
                _name.AddToClassList("hidden");
                _chips.AddToClassList("hidden");
                // Hidden but still laid out, so the columns stay aligned with the rows above and below.
                _dots.style.visibility = Visibility.Hidden;
                _locked.RemoveFromClassList("hidden");
                _locked.text = $"Unlocks at level {unlockLevel}";
            }

            public void ShowSpell(SpellDefinition spell, SpellUpgradeState upgrades, LevelUpConfig config)
            {
                Root.RemoveFromClassList("locked");
                _icon.RemoveFromClassList("icon-lock");
                _icon.style.backgroundImage = new StyleBackground(spell.Icon);
                _icon.style.unityBackgroundImageTintColor = spell.Color;
                ShowScaling(spell);
                _name.RemoveFromClassList("hidden");
                _name.text = spell.DisplayName;

                _chips.Clear();
                foreach (var definition in config.SpellUpgrades)
                {
                    var rank = definition != null ? upgrades.Rank(definition.Kind) : 0;
                    if (rank <= 0)
                        continue;
                    var numeral = rank <= Numerals.Length ? Numerals[rank - 1] : rank.ToString();
                    _chips.Add(CreateLabel($"{definition.DisplayName} {numeral}", "upgrade-chip"));
                }

                var hasUpgrades = _chips.childCount > 0;
                _chips.EnableInClassList("hidden", !hasUpgrades);
                _locked.EnableInClassList("hidden", hasUpgrades);
                _locked.text = hasUpgrades ? string.Empty : "No upgrades yet";
            }
        }
    }
}

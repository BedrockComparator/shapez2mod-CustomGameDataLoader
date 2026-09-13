using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CustomResourcesLoader.Builders
{
    /// <summary>
    /// Fluent builder for constructing <see cref="MetaShapeColorScheme"/> instances at runtime.
    /// </summary>
    /// <remarks>
    /// <para>Color schemes have complex visualization data (per-color materials per visualization type).
    /// The recommended approach is <see cref="CloneFrom"/> an existing scheme and then modify
    /// colors and mixing rules while preserving the visualization data.</para>
    /// <para>When using <see cref="Create"/>, you must supply visualization schemes via
    /// <see cref="SetVisualizationScheme"/> for each <see cref="ColorVisualizationSchemeType"/>.</para>
    /// </remarks>
    public class MetaShapeColorSchemeBuilder
    {
        public enum ColorType
        {
            Primary,
            Secondary,
            Tertiary
        }

        private MetaShapeColorScheme _instance;
        private List<MetaShapeColor> _primaryColors = new List<MetaShapeColor>();
        private List<MetaShapeColor> _secondaryColors = new List<MetaShapeColor>();
        private List<MetaShapeColor> _tertiaryColors = new List<MetaShapeColor>();
        private List<MetaShapeColor> _playerObtainableColors = new List<MetaShapeColor>();
        private MetaShapeColor _defaultColor;
        private List<(MetaShapeColor Color1, MetaShapeColor Color2, MetaShapeColor Result)> _mixResults
            = new List<(MetaShapeColor, MetaShapeColor, MetaShapeColor)>();

        private MetaShapeColorSchemeBuilder() { }

        // ── Entry points ──────────────────────────────────────

        /// <summary>Create a new empty MetaShapeColorScheme. You must supply visualization schemes.</summary>
        public static MetaShapeColorSchemeBuilder Create()
        {
            var b = new MetaShapeColorSchemeBuilder();
            b._instance = ScriptableObject.CreateInstance<MetaShapeColorScheme>();
            // ScriptableObject.CreateInstance does not call OnAfterDeserialize,
            // so EditorDict._CachedEntries is null; initialize it explicitly.
            b._instance.VisualizationSchemes._CachedEntries
                = new Dictionary<ColorVisualizationSchemeType, MetaShapeColorVisualizationScheme>();
            return b;
        }

        /// <summary>
        /// Clone from an existing MetaShapeColorScheme. Visualization data is preserved.
        /// This is the recommended path — modify colors and mixing rules on top of the clone.
        /// </summary>
        public static MetaShapeColorSchemeBuilder CloneFrom(MetaShapeColorScheme original)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            var b = new MetaShapeColorSchemeBuilder();
            b._instance = UnityEngine.Object.Instantiate(original);

            // Deep-clone visualization schemes so modifications don't affect the original
            foreach (var kvp in b._instance.VisualizationSchemes._CachedEntries.ToList())
            {
                b._instance.VisualizationSchemes._CachedEntries[kvp.Key] = UnityEngine.Object.Instantiate(kvp.Value);
            }

            b._primaryColors = new List<MetaShapeColor>(original.PrimaryColors);
            b._secondaryColors = new List<MetaShapeColor>(original.SecondaryColors);
            b._tertiaryColors = new List<MetaShapeColor>(original.TertiaryColors);
            b._playerObtainableColors = new List<MetaShapeColor>(original.PlayerObtainableColors);
            b._defaultColor = original.DefaultColor;
            b._mixResults = original.MixResults
                .Select(m => (m.Color1, m.Color2, m.ColorResult))
                .ToList();
            return b;
        }

        // ── Colors ────────────────────────────────────────────

        public MetaShapeColorSchemeBuilder SetDefaultColor(MetaShapeColor color)
        {
            _defaultColor = color ?? throw new ArgumentNullException(nameof(color));
            return this;
        }

        public MetaShapeColorSchemeBuilder AddPrimaryColor(MetaShapeColor color)
        {
            _primaryColors.Add(color ?? throw new ArgumentNullException(nameof(color)));
            return this;
        }

        public MetaShapeColorSchemeBuilder AddSecondaryColor(MetaShapeColor color)
        {
            _secondaryColors.Add(color ?? throw new ArgumentNullException(nameof(color)));
            return this;
        }

        public MetaShapeColorSchemeBuilder AddTertiaryColor(MetaShapeColor color)
        {
            _tertiaryColors.Add(color ?? throw new ArgumentNullException(nameof(color)));
            return this;
        }

        /// <summary>Mark a color as obtainable by the player. Must also be present in primary/secondary/tertiary colors.</summary>
        public MetaShapeColorSchemeBuilder AddPlayerObtainableColor(MetaShapeColor color)
        {
            _playerObtainableColors.Add(color ?? throw new ArgumentNullException(nameof(color)));
            return this;
        }

        /// <summary>
        /// Add a color to the scheme, routing it to the appropriate list based on <paramref name="type"/>.
        /// Optionally also marks it as player-obtainable (default true).
        /// </summary>
        public MetaShapeColorSchemeBuilder AddColor(MetaShapeColor color, ColorType type, bool playerObtainable = true)
        {
            if (color == null) throw new ArgumentNullException(nameof(color));

            switch (type)
            {
                case ColorType.Primary:
                    _primaryColors.Add(color);
                    break;
                case ColorType.Secondary:
                    _secondaryColors.Add(color);
                    break;
                case ColorType.Tertiary:
                    _tertiaryColors.Add(color);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            if (playerObtainable)
                _playerObtainableColors.Add(color);

            return this;
        }

        /// <summary>Remove all colors and mixing rules. Useful when starting from a clone but replacing colors entirely.</summary>
        public MetaShapeColorSchemeBuilder ClearColors()
        {
            _primaryColors.Clear();
            _secondaryColors.Clear();
            _tertiaryColors.Clear();
            _playerObtainableColors.Clear();
            _defaultColor = null;
            _mixResults.Clear();
            return this;
        }

        // ── Mixing rules ──────────────────────────────────────

        /// <summary>Define the mixing result of two colors. Every color pair must have exactly one result defined.</summary>
        public MetaShapeColorSchemeBuilder AddMixResult(MetaShapeColor color1, MetaShapeColor color2, MetaShapeColor result)
        {
            var existingPairs = new HashSet<(MetaShapeColor, MetaShapeColor)>();
            foreach (var m in _mixResults)
            {
                existingPairs.Add((m.Color1, m.Color2));
                existingPairs.Add((m.Color2, m.Color1));
            }
            if (existingPairs.Contains((color1, color2)))
            {
                throw new InvalidOperationException($"Unordered mixResult {color1.Code} + {color2.Code} already exists.");
            }
            _mixResults.Add((color1 ?? throw new ArgumentNullException(nameof(color1)),
                             color2 ?? throw new ArgumentNullException(nameof(color2)),
                             result ?? throw new ArgumentNullException(nameof(result))));
            return this;
        }

        /// <summary>Remove all mixing rules.</summary>
        public MetaShapeColorSchemeBuilder ClearMixResults()
        {
            _mixResults.Clear();
            return this;
        }

        /// <summary>
        /// Fill every color pair that does NOT yet have an explicit <see cref="AddMixResult"/>
        /// with <paramref name="defaultResult"/>. When <paramref name="defaultResult"/> is null,
        /// <see cref="DefaultColor"/> is used.
        /// </summary>
        public MetaShapeColorSchemeBuilder SetDefaultMixResult(MetaShapeColor? defaultResult = null)
        {
            var result = defaultResult ?? _defaultColor;
            if (result == null)
                throw new InvalidOperationException(
                    "DefaultColor must be set before calling SetDefaultMixResult without an explicit defaultResult.");

            var allColors = new HashSet<MetaShapeColor>();
            foreach (var c in _primaryColors) allColors.Add(c);
            foreach (var c in _secondaryColors) allColors.Add(c);
            foreach (var c in _tertiaryColors) allColors.Add(c);

            var existingPairs = new HashSet<(MetaShapeColor, MetaShapeColor)>();
            foreach (var m in _mixResults)
            {
                existingPairs.Add((m.Color1, m.Color2));
                existingPairs.Add((m.Color2, m.Color1));
            }

            foreach (var c1 in allColors)
            {
                foreach (var c2 in allColors)
                {
                    if (!existingPairs.Contains((c1, c2)))
                    {
                        _mixResults.Add((c1, c2, result));
                        existingPairs.Add((c1, c2));
                        existingPairs.Add((c2, c1));
                    }
                        
                }
            }

            return this;
        }

        // ── Visualization schemes ─────────────────────────────

        /// <summary>
        /// Set the visualization scheme for a given rendering type.
        /// Required when using <see cref="Create"/>; preserved automatically when using <see cref="CloneFrom"/>.
        /// </summary>
        public MetaShapeColorSchemeBuilder SetVisualizationScheme(
            ColorVisualizationSchemeType type,
            MetaShapeColorVisualizationScheme scheme)
        {
            _instance.VisualizationSchemes._CachedEntries[type] = scheme ?? throw new ArgumentNullException(nameof(scheme));
            return this;
        }

        /// <summary>
        /// Set the full <see cref="MetaShapeColorVisualizationScheme.ColorRenderData"/> for a color
        /// across one or more visualization scheme types. Creates the target visualization scheme
        /// if it does not already exist.
        /// </summary>
        public MetaShapeColorSchemeBuilder SetRenderData(
            MetaShapeColor color,
            IReadOnlyDictionary<ColorVisualizationSchemeType, MetaShapeColorVisualizationScheme.ColorRenderData> renderDataByScheme)
        {
            if (color == null) throw new ArgumentNullException(nameof(color));
            if (renderDataByScheme == null) throw new ArgumentNullException(nameof(renderDataByScheme));

            foreach (var kvp in renderDataByScheme)
            {
                EnsureVisualizationScheme(kvp.Key);
                _instance.VisualizationSchemes._CachedEntries[kvp.Key].RenderData._CachedEntries[color] = kvp.Value;
            }

            return this;
        }

        /// <summary>
        /// Set only the display <see cref="Color"/> for a color in one or more visualization scheme types,
        /// preserving the existing <see cref="MetaShapeColorRenderData"/>. If no render-data entry exists
        /// for the color yet, one is created with a null <see cref="MetaShapeColorRenderData"/>.
        /// </summary>
        public MetaShapeColorSchemeBuilder SetRenderColor(
            MetaShapeColor color,
            IReadOnlyDictionary<ColorVisualizationSchemeType, Color> colorsByScheme)
        {
            if (color == null) throw new ArgumentNullException(nameof(color));
            if (colorsByScheme == null) throw new ArgumentNullException(nameof(colorsByScheme));

            foreach (var kvp in colorsByScheme)
            {
                EnsureVisualizationScheme(kvp.Key);
                var renderDataDict = _instance.VisualizationSchemes._CachedEntries[kvp.Key].RenderData._CachedEntries;

                if (renderDataDict.TryGetValue(color, out var existing))
                {
                    existing.Color = kvp.Value;
                    renderDataDict[color] = existing;
                }
                else
                {
                    renderDataDict[color] = new MetaShapeColorVisualizationScheme.ColorRenderData
                    {
                        Color = kvp.Value,
                    };
                }
            }

            return this;
        }

        /// <summary>
        /// Copy all visualization-scheme render-data entries from one color to another.
        /// The target color's entries are replaced with the source color's values (struct copy).
        /// </summary>
        public MetaShapeColorSchemeBuilder CopyVisualizationScheme(MetaShapeColor from, MetaShapeColor to)
        {
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));

            foreach (var schemeKvp in _instance.VisualizationSchemes._CachedEntries)
            {
                var renderDataDict = schemeKvp.Value.RenderData._CachedEntries;
                if (renderDataDict.TryGetValue(from, out var renderData))
                {
                    renderDataDict[to] = renderData;
                }
            }

            return this;
        }

        private void EnsureVisualizationScheme(ColorVisualizationSchemeType type)
        {
            if (!_instance.VisualizationSchemes._CachedEntries.ContainsKey(type))
            {
                var vizScheme = ScriptableObject.CreateInstance<MetaShapeColorVisualizationScheme>();
                vizScheme.RenderData = new EditorDict<MetaShapeColor, MetaShapeColorVisualizationScheme.ColorRenderData>();
                vizScheme.RenderData._CachedEntries
                    = new Dictionary<MetaShapeColor, MetaShapeColorVisualizationScheme.ColorRenderData>();
                _instance.VisualizationSchemes._CachedEntries[type] = vizScheme;
            }
        }

        // ── Build ─────────────────────────────────────────────

        public MetaShapeColorScheme Build()
        {
            if (_defaultColor == null)
                throw new InvalidOperationException("DefaultColor must be set.");
            if (_primaryColors.Count == 0 && _secondaryColors.Count == 0 && _tertiaryColors.Count == 0)
                throw new InvalidOperationException("At least one color must be defined.");
            if (_playerObtainableColors.Count == 0)
                throw new InvalidOperationException("At least one player-obtainable color must be defined.");

            _instance.DefaultColor = _defaultColor;
            _instance.PrimaryColors = _primaryColors.ToList();
            _instance.SecondaryColors = _secondaryColors.ToList();
            _instance.TertiaryColors = _tertiaryColors.ToList();
            _instance.PlayerObtainableColors = _playerObtainableColors.ToList();
            _instance.MixResults = _mixResults
                .Select(m => new MetaShapeColorScheme.MixingData
                {
                    Color1 = m.Color1,
                    Color2 = m.Color2,
                    ColorResult = m.Result,
                })
                .ToArray();

            return _instance;
        }

        public MetaShapeColorScheme Build(string objectName)
        {
            _instance.name = objectName;
            return Build();
        }
    }
}

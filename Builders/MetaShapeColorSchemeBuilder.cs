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

        // ── Visualization schemes ─────────────────────────────

        /// <summary>
        /// Set the visualization scheme for a given rendering type.
        /// Required when using <see cref="Create"/>; preserved automatically when using <see cref="CloneFrom"/>.
        /// </summary>
        public MetaShapeColorSchemeBuilder SetVisualizationScheme(
            ColorVisualizationSchemeType type,
            MetaShapeColorVisualizationScheme scheme)
        {
            _instance.VisualizationSchemes[type] = scheme ?? throw new ArgumentNullException(nameof(scheme));
            return this;
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

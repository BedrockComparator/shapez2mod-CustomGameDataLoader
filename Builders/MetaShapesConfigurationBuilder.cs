using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CustomResourcesLoader.Builders
{
    /// <summary>
    /// Fluent builder for constructing <see cref="MetaShapesConfiguration"/> instances at runtime.
    /// </summary>
    public class MetaShapesConfigurationBuilder
    {
        private MetaShapesConfiguration _instance;
        private MetaShapeSubPart _pinPart;
        private MetaShapeSubPart _crystalPart;
        private List<(MetaShapeSubPart Part, MetaShapesConfiguration.PartGenerationRarity Rarity)> _parts
            = new List<(MetaShapeSubPart, MetaShapesConfiguration.PartGenerationRarity)>();

        private MetaShapesConfigurationBuilder() { }

        // ── Entry points ──────────────────────────────────────

        /// <summary>Create a new empty MetaShapesConfiguration.</summary>
        public static MetaShapesConfigurationBuilder Create()
        {
            var b = new MetaShapesConfigurationBuilder();
            b._instance = ScriptableObject.CreateInstance<MetaShapesConfiguration>();
            return b;
        }

        /// <summary>Clone from an existing MetaShapesConfiguration.</summary>
        public static MetaShapesConfigurationBuilder CloneFrom(MetaShapesConfiguration original)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            var b = new MetaShapesConfigurationBuilder();
            b._instance = UnityEngine.Object.Instantiate(original);
            b._pinPart = original.PinShapePart;
            b._crystalPart = original.CrystalShapePart;
            b._parts = original.Parts
                .Select(p => (p.Part, p.GenerationRarity))
                .ToList();
            return b;
        }

        // ── Properties ────────────────────────────────────────

        /// <summary>Set the number of layers per shape (must be even, 1-32).</summary>
        public MetaShapesConfigurationBuilder SetPartCount(int count)
        {
            if (count < 1 || count > 32)
                throw new ArgumentOutOfRangeException(nameof(count), "PartCount must be 1-32.");
            if (count % 2 != 0)
                throw new ArgumentException("PartCount must be even (required by the half-cutter mechanic).", nameof(count));
            _instance.PartCount = count;
            return this;
        }

        /// <summary>Designate an existing MetaShapeSubPart as the pin/pusher shape.</summary>
        public MetaShapesConfigurationBuilder SetPinShapePart(MetaShapeSubPart part)
        {
            _pinPart = part ?? throw new ArgumentNullException(nameof(part));
            return this;
        }

        /// <summary>Designate an existing MetaShapeSubPart as the crystal shape.</summary>
        public MetaShapesConfigurationBuilder SetCrystalShapePart(MetaShapeSubPart part)
        {
            _crystalPart = part ?? throw new ArgumentNullException(nameof(part));
            return this;
        }

        // ── Parts list ────────────────────────────────────────

        /// <summary>Add a shape part with the given generation rarity.</summary>
        public MetaShapesConfigurationBuilder AddPart(
            MetaShapeSubPart part,
            MetaShapesConfiguration.PartGenerationRarity rarity)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            _parts.Add((part, rarity));
            return this;
        }

        /// <summary>Remove all configured parts.</summary>
        public MetaShapesConfigurationBuilder ClearParts()
        {
            _parts.Clear();
            return this;
        }

        /// <summary>Remove a specific part by reference.</summary>
        public MetaShapesConfigurationBuilder RemovePart(MetaShapeSubPart part)
        {
            _parts.RemoveAll(p => p.Part == part);
            return this;
        }

        // ── Build ─────────────────────────────────────────────

        /// <summary>Build the MetaShapesConfiguration. Validates constraints.</summary>
        public MetaShapesConfiguration Build()
        {
            if (_pinPart == null)
                throw new InvalidOperationException("PinShapePart must be set.");
            if (_crystalPart == null)
                throw new InvalidOperationException("CrystalShapePart must be set.");
            if (_parts.Count == 0)
                throw new InvalidOperationException("At least one part must be added.");
            if (_parts.All(p => p.Part != _pinPart))
                throw new InvalidOperationException("PinShapePart must be present in the Parts list.");
            if (_parts.All(p => p.Part != _crystalPart))
                throw new InvalidOperationException("CrystalShapePart must be present in the Parts list.");

            _instance.PinShapePart = _pinPart;
            _instance.CrystalShapePart = _crystalPart;
            _instance.Parts = _parts
                .Select(p => new MetaShapesConfiguration.GenerationPart
                {
                    Part = p.Part,
                    GenerationRarity = p.Rarity,
                })
                .ToArray();

            return _instance;
        }

        /// <summary>Build and assign the object name.</summary>
        public MetaShapesConfiguration Build(string objectName)
        {
            _instance.name = objectName;
            return Build();
        }
    }
}

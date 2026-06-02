using System;
using UnityEngine;

namespace CustomResourcesLoader.Builders
{
    /// <summary>
    /// Fluent builder for constructing <see cref="MetaShapeColor"/> instances at runtime.
    /// </summary>
    public class MetaShapeColorBuilder
    {
        private MetaShapeColor _instance;

        private MetaShapeColorBuilder() { }

        // ── Entry points ──────────────────────────────────────

        /// <summary>Create a new empty MetaShapeColor.</summary>
        public static MetaShapeColorBuilder Create()
        {
            var b = new MetaShapeColorBuilder();
            b._instance = ScriptableObject.CreateInstance<MetaShapeColor>();
            b._instance.Material = ShapeShaderMaterialType.NormalColor;
            return b;
        }

        /// <summary>Clone from an existing MetaShapeColor.</summary>
        public static MetaShapeColorBuilder CloneFrom(MetaShapeColor original)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            var b = new MetaShapeColorBuilder();
            b._instance = UnityEngine.Object.Instantiate(original);
            return b;
        }

        // ── Properties ────────────────────────────────────────

        /// <summary>Set the single-character color code (e.g. 'r', 'g', 'b', 'c', 'm', 'y', 'w').</summary>
        public MetaShapeColorBuilder SetCode(char code)
        {
            _instance._Code = code;
            return this;
        }

        /// <summary>Set the shader material type for rendering this color.</summary>
        public MetaShapeColorBuilder SetMaterial(ShapeShaderMaterialType material)
        {
            _instance.Material = material;
            return this;
        }

        // ── Build ─────────────────────────────────────────────

        public MetaShapeColor Build()
        {
            return _instance;
        }

        public MetaShapeColor Build(string objectName)
        {
            _instance.name = objectName;
            return _instance;
        }
    }
}

using System;
using System.Collections.Generic;
using Game.Core.HUD;
using Game.Core.Research;
using UnityEngine;
using UnityEngine.Video;

namespace CustomResourcesLoader.Builders
{
    /// <summary>
    /// Fluent builder for constructing <see cref="MetaVideoDefinition"/> instances at runtime.
    /// </summary>
    public class MetaVideoDefinitionBuilder
    {
        private MetaVideoDefinition _instance;
        private List<MetaVideoDefinition.Marker> _markers;

        private MetaVideoDefinitionBuilder() { }

        // ── Entry points ──────────────────────────────────────

        /// <summary>Create a new empty MetaVideoDefinition.</summary>
        public static MetaVideoDefinitionBuilder Create()
        {
            var b = new MetaVideoDefinitionBuilder();
            b._instance = ScriptableObject.CreateInstance<MetaVideoDefinition>();
            b._markers = new List<MetaVideoDefinition.Marker>();
            return b;
        }

        /// <summary>Clone from an existing MetaVideoDefinition. Modifications only affect the copy.</summary>
        public static MetaVideoDefinitionBuilder CloneFrom(MetaVideoDefinition original)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            var b = new MetaVideoDefinitionBuilder();
            b._instance = UnityEngine.Object.Instantiate(original);
            b._markers = new List<MetaVideoDefinition.Marker>(original.Markers);
            return b;
        }

        /// <summary>
        /// Clone from a MetaVideoDefinition already registered in GameData.
        /// </summary>
        public static MetaVideoDefinitionBuilder CloneFrom(GameData gameData, GameVideoId id)
        {
            if (gameData == null) throw new ArgumentNullException(nameof(gameData));
            return CloneFrom(gameData.GetVideo(id));
        }

        // ── Properties ────────────────────────────────────────

        /// <summary>Set the VideoClip asset.</summary>
        public MetaVideoDefinitionBuilder SetVideo(VideoClip clip)
        {
            _instance.Video = clip;
            return this;
        }

        // ── Markers ───────────────────────────────────────────

        /// <summary>Replace all markers with the given array.</summary>
        public MetaVideoDefinitionBuilder SetMarkers(MetaVideoDefinition.Marker[] markers)
        {
            _markers = new List<MetaVideoDefinition.Marker>(markers ?? Array.Empty<MetaVideoDefinition.Marker>());
            return this;
        }

        /// <summary>Add a marker configured via sub-builder callback.</summary>
        public MetaVideoDefinitionBuilder AddMarker(Action<MarkerBuilder> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));
            var mb = new MarkerBuilder();
            configure(mb);
            _markers.Add(mb.Build());
            return this;
        }

        /// <summary>Remove all markers.</summary>
        public MetaVideoDefinitionBuilder ClearMarkers()
        {
            _markers.Clear();
            return this;
        }

        // ── Build ─────────────────────────────────────────────

        /// <summary>Build the MetaVideoDefinition with current property values.</summary>
        public MetaVideoDefinition Build()
        {
            _instance.Markers = _markers.ToArray();
            return _instance;
        }

        /// <summary>Build and assign the object name (which determines <see cref="MetaVideoDefinition.Id"/>).</summary>
        public MetaVideoDefinition Build(string objectName)
        {
            _instance.name = objectName;
            return Build();
        }

        // ── Marker sub-builder ────────────────────────────────

        public class MarkerBuilder
        {
            private string _keybindingId = string.Empty;
            private float _startTimeMs;
            private float _durationMs;
            private bool _visibleWhileHidden;
            private HudVideoMarkerPosition _position;

            /// <summary>Set the input keybinding hint ID (e.g. "interact.confirm").</summary>
            public MarkerBuilder SetKeybinding(string keybindingId)
            {
                _keybindingId = keybindingId ?? string.Empty;
                return this;
            }

            /// <summary>Marker start time in milliseconds.</summary>
            public MarkerBuilder SetStartTime(float ms)
            {
                _startTimeMs = Math.Max(0, ms);
                return this;
            }

            /// <summary>Marker display duration in milliseconds.</summary>
            public MarkerBuilder SetDuration(float ms)
            {
                _durationMs = Math.Max(0, ms);
                return this;
            }

            /// <summary>Whether the marker is visible even when the HUD video overlay is hidden.</summary>
            public MarkerBuilder SetVisibleWhileHidden(bool visible)
            {
                _visibleWhileHidden = visible;
                return this;
            }

            /// <summary>Screen position of the marker.</summary>
            public MarkerBuilder SetPosition(HudVideoMarkerPosition position)
            {
                _position = position;
                return this;
            }

            public MetaVideoDefinition.Marker Build()
            {
                return new MetaVideoDefinition.Marker
                {
                    KeybindingId = _keybindingId,
                    StartTimeMs = _startTimeMs,
                    DurationMs = _durationMs,
                    VisibleWhileHidden = _visibleWhileHidden,
                    Position = _position,
                };
            }
        }
    }
}

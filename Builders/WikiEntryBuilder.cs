using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Localization;
using Game.Core.Research;
using Game.Core.Research.Content;
using Game.Core.Tutorial.Runtime;
using UnityEngine;

namespace CustomResourcesLoader.Builders
{
    /// <summary>
    /// Fluent builder for constructing <see cref="MetaWikiEntry"/> instances at runtime,
    /// without Unity Editor asset workflow.
    /// </summary>
    /// <example>
    /// var entry = WikiEntryBuilder.Create("MyEntry")
    ///     .ShowInKnowledgePanel()
    ///     .AddRelatedEntry(new WikiEntryId("OtherEntry"))
    ///     .AddHeading("Welcome")
    ///     .AddText("This describes the building.")
    ///     .AddImage(mySprite)
    ///     .Build();
    /// </example>
    public class WikiEntryBuilder
    {
        private MetaWikiEntry _entry;
        private List<IWikiEntryContent> _contents;
        private List<WikiEntryId> _relatedEntries;

        private WikiEntryBuilder() { }

        // ── Entry points ──────────────────────────────────────

        /// <summary>Create a new empty MetaWikiEntry with the given ID.</summary>
        public static WikiEntryBuilder Create(string entryId)
        {
            if (string.IsNullOrEmpty(entryId))
                throw new ArgumentException("entryId must not be null or empty.", nameof(entryId));

            var b = new WikiEntryBuilder();
            b._entry = ScriptableObject.CreateInstance<MetaWikiEntry>();
            b._entry.name = entryId;
            b._contents = new List<IWikiEntryContent>();
            b._relatedEntries = new List<WikiEntryId>();
            return b;
        }

        /// <summary>Clone from an existing MetaWikiEntry. Modifications only affect the copy.</summary>
        public static WikiEntryBuilder CloneFrom(MetaWikiEntry original)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));

            var b = new WikiEntryBuilder();
            b._entry = UnityEngine.Object.Instantiate(original);
            b._contents = new List<IWikiEntryContent>();
            b._relatedEntries = new List<WikiEntryId>(original.RelatedEntries);

            // Deep-clone existing contents: go through each Meta*Data, call Create() to get runtime content
            foreach (var contentData in original.Contents)
            {
                if (contentData != null)
                    b._contents.Add(contentData.Create());
            }

            return b;
        }

        /// <summary>Clone from a MetaWikiEntry already registered in GameData.</summary>
        public static WikiEntryBuilder CloneFrom(GameData gameData, WikiEntryId id)
        {
            if (gameData == null) throw new ArgumentNullException(nameof(gameData));
            return CloneFrom(gameData.GetWikiEntry(id));
        }

        // ── Entry-level settings ──────────────────────────────

        /// <summary>Control visibility of this entry in the knowledge panel (default: true).</summary>
        public WikiEntryBuilder ShowInKnowledgePanel(bool show = true)
        {
            _entry.ShowInKnowledgePanel = show;
            return this;
        }

        /// <summary>Add a related wiki entry by ID (appends to existing).</summary>
        public WikiEntryBuilder AddRelatedEntry(WikiEntryId id)
        {
            _relatedEntries.Add(id);
            return this;
        }

        /// <summary>Replace all related entries.</summary>
        public WikiEntryBuilder SetRelatedEntries(params WikiEntryId[] ids)
        {
            _relatedEntries = new List<WikiEntryId>(ids ?? Array.Empty<WikiEntryId>());
            return this;
        }

        // ── Content: Heading ──────────────────────────────────

        public WikiEntryBuilder AddHeading(IText text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            _contents.Add(new WikiEntryContentHeading(text));
            return this;
        }

        public WikiEntryBuilder AddHeading(string plainText)
        {
            return AddHeading(new RawStringText(plainText));
        }

        public WikiEntryBuilder AddHeadingLocalized(string translationKey)
        {
            if (string.IsNullOrEmpty(translationKey))
                throw new ArgumentException("translationKey must not be null or empty.", nameof(translationKey));
            return AddHeading(new LazyLocalizedText(new TranslationId(translationKey)));
        }

        // ── Content: Text ─────────────────────────────────────

        public WikiEntryBuilder AddText(IText text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            _contents.Add(new WikiEntryContentText(text));
            return this;
        }

        public WikiEntryBuilder AddText(string plainText)
        {
            return AddText(new RawStringText(plainText));
        }

        public WikiEntryBuilder AddTextLocalized(string translationKey)
        {
            if (string.IsNullOrEmpty(translationKey))
                throw new ArgumentException("translationKey must not be null or empty.", nameof(translationKey));
            return AddText(new LazyLocalizedText(new TranslationId(translationKey)));
        }

        // ── Content: NarrativeText ────────────────────────────

        public WikiEntryBuilder AddNarrativeText(IText text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            _contents.Add(new WikiEntryContentNarrativeText(text));
            return this;
        }

        public WikiEntryBuilder AddNarrativeText(string plainText)
        {
            return AddNarrativeText(new RawStringText(plainText));
        }

        public WikiEntryBuilder AddNarrativeTextLocalized(string translationKey)
        {
            if (string.IsNullOrEmpty(translationKey))
                throw new ArgumentException("translationKey must not be null or empty.", nameof(translationKey));
            return AddNarrativeText(new LazyLocalizedText(new TranslationId(translationKey)));
        }

        // ── Content: ShapeCodes ───────────────────────────────

        /// <summary>Add a shape-codes visualization block. No parameters required.</summary>
        public WikiEntryBuilder AddShapeCodes()
        {
            _contents.Add(new WikiEntryContentShapeCodes());
            return this;
        }

        // ── Content: Image ────────────────────────────────────

        public WikiEntryBuilder AddImage(Sprite image)
        {
            _contents.Add(new WikiEntryContentImage(image));
            return this;
        }

        // ── Content: Blueprint ────────────────────────────────

        public WikiEntryBuilder AddBlueprint(Sprite image, string blueprintCode)
        {
            _contents.Add(new WikiEntryContentBlueprint(image, blueprintCode ?? string.Empty));
            return this;
        }

        // ── Content: Video ────────────────────────────────────

        /// <summary>Add a video content block referencing a MetaVideoDefinition.</summary>
        public WikiEntryBuilder AddVideo(MetaVideoDefinition video)
        {
            if (video == null) throw new ArgumentNullException(nameof(video));
            _contents.Add(new WikiEntryContentVideo(video));
            return this;
        }

        // ── Content: TutorialPopup ────────────────────────────

        /// <summary>Add a tutorial popup trigger. Requires a MetaTutorialPopup reference.</summary>
        public WikiEntryBuilder AddTutorialPopup(MetaTutorialPopup popup)
        {
            if (popup == null) throw new ArgumentNullException(nameof(popup));
            _contents.Add(new WikiEntryContentTutorialPopup(popup));
            return this;
        }

        // ── Content: BuildingPanel ────────────────────────────

        public WikiEntryBuilder AddBuildingPanel(BuildingDefinitionGroupId buildingId, IText text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            _contents.Add(new WikiEntryContentBuildingPanel(buildingId, text));
            return this;
        }

        public WikiEntryBuilder AddBuildingPanel(BuildingDefinitionGroupId buildingId, string plainText)
        {
            return AddBuildingPanel(buildingId, new RawStringText(plainText));
        }

        public WikiEntryBuilder AddBuildingPanelLocalized(BuildingDefinitionGroupId buildingId, string translationKey)
        {
            if (string.IsNullOrEmpty(translationKey))
                throw new ArgumentException("translationKey must not be null or empty.", nameof(translationKey));
            return AddBuildingPanel(buildingId, new LazyLocalizedText(new TranslationId(translationKey)));
        }

        // ── Content: IslandPanel ──────────────────────────────

        public WikiEntryBuilder AddIslandPanel(MetaIslandDefinitionId islandId, IText text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            _contents.Add(new WikiEntryContentIslandPanel(islandId, text));
            return this;
        }

        public WikiEntryBuilder AddIslandPanel(MetaIslandDefinitionId islandId, string plainText)
        {
            return AddIslandPanel(islandId, new RawStringText(plainText));
        }

        public WikiEntryBuilder AddIslandPanelLocalized(MetaIslandDefinitionId islandId, string translationKey)
        {
            if (string.IsNullOrEmpty(translationKey))
                throw new ArgumentException("translationKey must not be null or empty.", nameof(translationKey));
            return AddIslandPanel(islandId, new LazyLocalizedText(new TranslationId(translationKey)));
        }

        // ── Content: TextWithLinks ────────────────────────────

        public WikiEntryBuilder AddTextWithLinks(IText text, params WikiEntryContentTextWithLinks.Link[] links)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            _contents.Add(new WikiEntryContentTextWithLinks(text, links ?? Array.Empty<WikiEntryContentTextWithLinks.Link>()));
            return this;
        }

        public WikiEntryBuilder AddTextWithLinks(string plainText, params WikiEntryContentTextWithLinks.Link[] links)
        {
            return AddTextWithLinks(new RawStringText(plainText), links);
        }

        /// <summary>Add a text-with-links block, specifying links as (linkId, linkUrl) tuples.</summary>
        public WikiEntryBuilder AddTextWithLinks(IText text, params (string linkId, string linkUrl)[] links)
        {
            var linkObjs = (links ?? Array.Empty<(string, string)>())
                .Select(l => new WikiEntryContentTextWithLinks.Link(l.linkId, l.linkUrl))
                .ToArray();
            return AddTextWithLinks(text, linkObjs);
        }

        /// <summary>Add a text-with-links block with plain text, specifying links as (linkId, linkUrl) tuples.</summary>
        public WikiEntryBuilder AddTextWithLinks(string plainText, params (string linkId, string linkUrl)[] links)
        {
            return AddTextWithLinks(new RawStringText(plainText), links);
        }

        // ── Content: LinearUpgrade ────────────────────────────

        public WikiEntryBuilder AddLinearUpgrade(ResearchLinearUpgradeId upgradeId)
        {
            _contents.Add(new WikiEntryContentLinearUpgrade(upgradeId));
            return this;
        }

        // ── Content: SideUpgrade ──────────────────────────────

        public WikiEntryBuilder AddSideUpgrade(ResearchUpgradeId upgradeId)
        {
            _contents.Add(new WikiEntryContentSideUpgrade(upgradeId));
            return this;
        }

        // ── Content: Generic escape hatch ─────────────────────

        /// <summary>
        /// Add an arbitrary IWikiEntryContent directly.
        /// Use this for content types that don't have dedicated builder methods
        /// (e.g. conditional types like ModeSpecific or ShapePartCountSpecific).
        /// </summary>
        public WikiEntryBuilder AddContent(IWikiEntryContent content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            _contents.Add(content);
            return this;
        }

        // ── Build ─────────────────────────────────────────────

        /// <summary>Build the MetaWikiEntry with all configured content.</summary>
        public MetaWikiEntry Build()
        {
            _entry.RelatedEntries = _relatedEntries.ToArray();
            _entry.Contents = _contents
                .Select(c => (IWikiEntryContentData)new RuntimeWikiContentData(c))
                .ToArray();
            return _entry;
        }

        // ── Internal helpers ──────────────────────────────────

        /// <summary>
        /// Runtime wrapper that adapts a pre-built <see cref="IWikiEntryContent"/>
        /// to the <see cref="IWikiEntryContentData"/> interface expected by MetaWikiEntry.
        /// Bypasses the Unity serialization layer entirely.
        /// </summary>
        private sealed class RuntimeWikiContentData : IWikiEntryContentData
        {
            private readonly IWikiEntryContent _content;
            public RuntimeWikiContentData(IWikiEntryContent content) => _content = content;
            public IWikiEntryContent Create() => _content;
        }

        /// <summary>
        /// Minimal <see cref="IText"/> implementation for plain, non-localized strings.
        /// </summary>
        private sealed class RawStringText : IText
        {
            private readonly string _value;
            public RawStringText(string value) => _value = value ?? string.Empty;

            public void Build(StringBuilder sb, ILocalizationResolver resolver, ITextStyleProvider textStyle)
            {
                sb.Append(_value);
            }

            public IText Bind(string placeholder, IText value)
            {
                // Plain text has no placeholders; return self unchanged.
                return this;
            }
        }
    }
}

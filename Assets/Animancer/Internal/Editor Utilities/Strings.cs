// Animancer // Copyright 2020 Kybernetik //

namespace Animancer
{
    /// <summary>Various string constants used throughout Animancer.</summary>
    public static class Strings
    {
        /************************************************************************************************************************/

        /// <summary>The standard prefix for <see cref="UnityEngine.CreateAssetMenuAttribute.menuName"/>.</summary>
        public const string MenuPrefix = "Animancer/";

        /// <summary>
        /// The base value for <see cref="UnityEngine.CreateAssetMenuAttribute.order"/> to group
        /// "Assets/Create/Animancer/..." menu items just under "Avatar Mask".
        /// </summary>
        public const int AssetMenuOrder = 410;

        /************************************************************************************************************************/

        /// <summary>The URL of the website where the Animancer documentation is hosted.</summary>
        public const string DocumentationURL = "https://kybernetik.com.au/animancer";

        /// <summary>The URL of the website where the Animancer API documentation is hosted.</summary>
        public const string APIDocumentationURL = DocumentationURL + "/api/Animancer";

        /// <summary>The email address which handles support for Animancer.</summary>
        public const string DeveloperEmail = "animancer@kybernetik.com.au";

        /************************************************************************************************************************/

        /// <summary>The conditional compilation symbol for editor-only code.</summary>
        public const string EditorOnly = "UNITY_EDITOR";

        /// <summary>The conditional compilation symbol for assertions.</summary>
        public const string Assert = "UNITY_ASSERTIONS";

        /************************************************************************************************************************/

        /// <summary>[Internal]
        /// A prefix for tooltips on Pro-Only features.
        /// <para></para>
        /// "[Pro-Only] " in Animancer Lite or "" in Animancer Pro.
        /// </summary>
        internal const string ProOnlyTag = "";

        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        /// <summary>[Editor-Only] URLs of various documentation pages.</summary>
        public static class DocsURLs
        {
            /************************************************************************************************************************/
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member.
            /************************************************************************************************************************/

            public const string DocsURL = DocumentationURL + "/docs/";

            public const string AnimatorControllers = DocsURL + "manual/animator-controllers";

            public const string Fading = DocsURL + "manual/blending/fading";

            public const string AnimationTypes = DocsURL + "manual/playing/inspector#animation-types";

            public const string EndEvents = DocsURL + "manual/events/end";

            public const string UpdateModes = DocsURL + "bugs/update-modes";

            public const string ChangeLogPrefix = DocsURL + "changes/animancer-";

            /************************************************************************************************************************/
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member.
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
    }
}


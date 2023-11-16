using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Shaders.Editor
{
    public struct ShaderEditorHelpers
    {
        /// <summary>
        /// Sets multiple keyword states on multiple materials.
        /// </summary>
        public static void SetKeywords(MaterialEditor m, List<KeywordState> states)
        {
            foreach (Material mat in m.targets)
            {
                foreach (KeywordState state in states)
                {
                    SetKeyword(m, state);
                }
            }
        }

        /// <summary>
        /// Sets multiple keyword states on multiple materials.
        /// </summary>
        public static void SetKeywords(Material m, List<KeywordState> states)
        {
            foreach (KeywordState state in states)
            {
                SetKeyword(m, state);
            }
        }

        /// <summary>
        /// Sets the keyword state on multiple materials.
        /// </summary>
        public static void SetKeyword(MaterialEditor m, KeywordState state)
        {
            SetKeyword(m, state.Keyword, state.State);
        }

        /// <summary>
        /// Sets the keyword state on multiple materials.
        /// </summary>
        public static void SetKeyword(MaterialEditor m, string kw, bool state)
        {
            foreach (Material mat in m.targets) SetKeyword(mat, kw, state);
        }

        /// <summary>
        /// Sets the keyword state on a material.
        /// </summary>
        public static void SetKeyword(Material m, KeywordState state)
        {
            SetKeyword(m, state.Keyword, state.State);
        }

        /// <summary>
        /// Sets the keyword state on a material.
        /// </summary>
        public static void SetKeyword(Material m, string kw, bool state)
        {
            if (state) m.EnableKeyword(kw);
            else m.DisableKeyword(kw);
        }

        /// <summary>
        /// Gets the keyword state on a material.
        /// </summary>
        public static bool GetKeyword(Material m, string kw)
        {
            return m.IsKeywordEnabled(kw);
        }

        /// <summary>
        /// Represents a keyword state that will be set on a material.
        /// </summary>
        public struct KeywordState
        {
            public KeywordState(string keyword, bool state)
            {
                Keyword = keyword;
                State = state;
            }

            public string Keyword;
            public bool State;
        }

        public struct KeywordStateBuilder
        {
            private List<KeywordState> _states;
            public List<KeywordState> States => _states ?? new List<KeywordState>();

            private void CreateList()
            {
                if (_states == null) _states = new List<KeywordState>();
            }

            public void Add(string keyword, bool state)
            {
                CreateList();

                _states.Add(new KeywordState(keyword, state));
            }
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TK_Engine
{
   /// <summary>
    /// 用于序列化和反序列化多语言数据的容器类
    /// Unity的JsonUtility不能直接序列化Dictionary，所以需要这个包装类
    /// </summary>
    [Serializable]
    public class MultiLanguageContainer
    {
        [Serializable]
        public class LanguageTextModePair
        {
            public Language language;
            public TextMode textMode;
            
            public LanguageTextModePair() { }
            
            public LanguageTextModePair(Language lang, TextMode tm)
            {
                language = lang;
                textMode = tm;
            }
        }
        
        public List<LanguageTextModePair> languagePairs = new List<LanguageTextModePair>();
        
        /// <summary>
        /// 将List转换为Dictionary，方便程序使用
        /// </summary>
        public Dictionary<Language, TextMode> ToDictionary()
        {
            Dictionary<Language, TextMode> dict = new Dictionary<Language, TextMode>();
            foreach (var pair in languagePairs)
            {
                dict[pair.language] = pair.textMode;
            }
            return dict;
        }
        
        /// <summary>
        /// 从Dictionary转换回List，用于序列化
        /// </summary>
        public void FromDictionary(Dictionary<Language, TextMode> dict)
        {
            languagePairs.Clear();
            foreach (var kvp in dict)
            {
                languagePairs.Add(new LanguageTextModePair(kvp.Key, kvp.Value));
            }
        }
        
        /// <summary>
        /// 获取指定语言的TextMode
        /// </summary>
        public TextMode GetTextMode(Language language)
        {
            foreach (var pair in languagePairs)
            {
                if (pair.language == language)
                    return pair.textMode;
            }
            return null;
        }
        
        /// <summary>
        /// 添加或更新语言数据
        /// </summary>
        public void SetTextMode(Language language, TextMode textMode)
        {
            for (int i = 0; i < languagePairs.Count; i++)
            {
                if (languagePairs[i].language == language)
                {
                    languagePairs[i].textMode = textMode;
                    return;
                }
            }
            languagePairs.Add(new LanguageTextModePair(language, textMode));
        }
        
        /// <summary>
        /// 检查是否包含指定语言
        /// </summary>
        public bool ContainsLanguage(Language language)
        {
            foreach (var pair in languagePairs)
            {
                if (pair.language == language)
                    return true;
            }
            return false;
        }
    }
}

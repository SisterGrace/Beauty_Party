Character Sets Generation
=========================

This folder contains character set files for creating Font Assets (TextMeshPro).

Files:
1. European Languages (English, German, French, etc.):
   - Contains Basic ASCII + Language specific characters (Accents, Umlauts, etc.)

2. Korean.txt:
   - Contains Basic ASCII + Hangul Jamo + Full Hangul Syllables (11,172 characters).

3. Japanese_Kana_Basic.txt:
   - Contains Basic ASCII + Hiragana + Katakana + Punctuation.
   - For Kanji, please use the "Chinese_Full_CJK.txt" file as it contains the shared CJK characters.

4. Chinese_Full_CJK.txt:
   - Contains Basic ASCII + CJK Symbols/Punctuation + Fullwidth Forms.
   - Contains the full "CJK Unified Ideographs" block (20,992 characters).
   - This covers:
     - Simplified Chinese (Common 3500 & General Standard)
     - Traditional Chinese (Common Big5)
     - Japanese Kanji (Joyo)
     - Common Hanja

Usage:
Select the text file corresponding to your language when creating a Font Asset in Unity.
For a global font, you can combine these contents or use the CJK file as a base.

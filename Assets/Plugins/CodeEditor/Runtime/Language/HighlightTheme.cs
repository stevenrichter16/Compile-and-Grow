using UnityEngine;

namespace CodeEditor.Language
{
    [System.Serializable]
    public sealed class HighlightTheme
    {
        public Color32 DefaultColor         = new Color32(212, 212, 212, 255);
        public Color32 KeywordColor         = new Color32( 86, 156, 214, 255);
        public Color32 StringColor          = new Color32(206, 145, 120, 255);
        public Color32 NumberColor          = new Color32(181, 206, 168, 255);
        public Color32 CommentColor         = new Color32(106, 153,  85, 255);
        public Color32 OperatorColor        = new Color32(212, 212, 212, 255);
        public Color32 TypeColor            = new Color32( 78, 201, 176, 255);
        public Color32 FunctionColor        = new Color32(220, 220, 170, 255);
        public Color32 VariableColor        = new Color32(156, 220, 254, 255);
        public Color32 ErrorColor           = new Color32(244,  71,  71, 255);
        public Color32 DecoratorColor       = new Color32(220, 220, 170, 255);
        public Color32 BiologicalKeywordColor = new Color32(197, 134, 192, 255);

        public Color32 GetColor(TokenCategory category)
        {
            switch (category)
            {
                case TokenCategory.Keyword:            return KeywordColor;
                case TokenCategory.String:             return StringColor;
                case TokenCategory.Number:             return NumberColor;
                case TokenCategory.Comment:            return CommentColor;
                case TokenCategory.Operator:           return OperatorColor;
                case TokenCategory.Type:               return TypeColor;
                case TokenCategory.Function:           return FunctionColor;
                case TokenCategory.Variable:           return VariableColor;
                case TokenCategory.Error:              return ErrorColor;
                case TokenCategory.Decorator:          return DecoratorColor;
                case TokenCategory.BiologicalKeyword:  return BiologicalKeywordColor;
                default:                               return DefaultColor;
            }
        }
    }
}

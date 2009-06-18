
using System;
using NUnit.Framework;
using Synpl.Parser.Sexp;
using System.Collections.Generic;
using Synpl.Core;
using System.Text;

namespace Synpl.Test.Core
{
    
    [TestFixture]
    public class TestSexpParser
    {
        [Test]
        public void TestTokenizer()
        {
            Synpl.Core.Parser parser = SexpParser.Instance;
            List<CharWithPosition> text = EnumerateString("(+ 1 2)");
            List<Token> tokens = parser.TokenizerFunc(text);
            
            List<Token> expectedTokens = new List<Token>();
            expectedTokens.Add(new Token((int)SexpParser.TokenTypes.OpenParen, "(", 0, 1));
            expectedTokens.Add(new Token((int)SexpParser.TokenTypes.Atom, "+", 1, 2));
            expectedTokens.Add(new Token((int)SexpParser.TokenTypes.Atom, "1", 3, 4));
            expectedTokens.Add(new Token((int)SexpParser.TokenTypes.Atom, "2", 5, 6));
            expectedTokens.Add(new Token((int)SexpParser.TokenTypes.CloseParen, ")", 6, 7));
            
            Assert.AreEqual(expectedTokens, tokens);
        }

        #region Private Helper Methods
        // The following functions are debugging helpers, they are not
        // used normally.
        #pragma warning disable 0169
        private string DumpCollection<T>(List<T> collection)
        {
            StringBuilder sb = new StringBuilder();
            foreach (T item in collection)
            {
                sb.Append(item.ToString());
                sb.Append("\n");
            }
            return sb.ToString();
        }
        
        private List<CharWithPosition> EnumerateString(string str)
        {
            List<CharWithPosition> result = new List<CharWithPosition>();
            for (int i = 0; i < str.Length; i++)
            {
                result.Add(new CharWithPosition(i, str[i]));
            }
            return result;
        }
        #endregion
    }
}


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
            Synpl.Core.Parser parser = SexpParser.GetInstance();
            CowList<CharWithPosition> text = EnumerateString("(+ 1 2)");
            CowList<Token> tokens = parser.TokenizerFunc(text);
            
            CowList<Token> expectedTokens = new CowList<Token>();
            expectedTokens.Add(new Token((int)SexpParser.TokenTypes.OpenParen, "(", 0, 1));
            expectedTokens.Add(new Token((int)SexpParser.TokenTypes.Atom, "+", 1, 2));
            expectedTokens.Add(new Token((int)SexpParser.TokenTypes.Atom, "1", 3, 4));
            expectedTokens.Add(new Token((int)SexpParser.TokenTypes.Atom, "2", 5, 6));
            expectedTokens.Add(new Token((int)SexpParser.TokenTypes.CloseParen, ")", 6, 7));
            
            Assert.AreEqual(expectedTokens, tokens);
        }        

        [Test]
        public void TestParserBasic()
        {
            ParseTree parseTree = GetTree("(+ 1 2)");
            string expectedTreeRepr = 
@"(0,7): () (list)
  (1,2): +
  (3,4): 1
  (5,6): 2
";
            Assert.AreEqual(expectedTreeRepr, parseTree.ToStringAsTree());
        }

        [Test]
        public void TestParserAdvanced()
        {
            ParseTree parseTree = GetTree("(map (lambda (x) (* x x)) '(1 2 3 4 5))");
            string expectedTreeRepr =
@"(0,39): () (list)
  (1,4): map
  (5,25): () (list)
    (6,12): lambda
    (13,16): () (list)
      (14,15): x
    (17,24): () (list)
      (18,19): *
      (20,21): x
      (22,23): x
  (26,38): ' (quote)
    (27,38): () (list)
      (28,29): 1
      (30,31): 2
      (32,33): 3
      (34,35): 4
      (36,37): 5
";
            Assert.AreEqual(expectedTreeRepr, parseTree.ToStringAsTree());
        }

        [Test]
        public void TestPathFunctions()
        {
            string sourceCode =
@"
(map (lambda (x) (* x x))
     '(1 2 3))  
";
            ParseTree parseTree = GetTree(sourceCode);
            CowList<Token> tokens = GetTokens(sourceCode);
            int lastAtomPosition = 0;
            foreach (Token token in tokens)
            {
                if ((SexpParser.TokenTypes)token.Kind == SexpParser.TokenTypes.Atom)
                {
                    lastAtomPosition = token.StartPosition;
                }
            }
            CowList<ParseTree> pathToAtom = parseTree.GetPathForPosition(lastAtomPosition);
            Assert.AreEqual(4, pathToAtom.Count);
            ParseTree ptAtom = pathToAtom[pathToAtom.Count - 1];
            Assert.IsTrue(ptAtom is ParseTreeAtom);
            ParseTreeAtom atom = ptAtom as ParseTreeAtom;
            Assert.AreEqual(38, atom.StartPosition);
            Assert.AreEqual(39, atom.EndPosition);
            Assert.AreEqual("3", atom.Content);
            
            CowList<int> path = atom.GetPath();
            Assert.AreEqual(new CowList<int>(2, 0, 2), path);
            
            ParseTree ptAtom2 = parseTree.GetNodeAtPath(path);
            Assert.IsTrue(ptAtom2 is ParseTreeAtom);
            ParseTreeAtom atom2 = ptAtom2 as ParseTreeAtom;
            Assert.AreEqual(atom.StartPosition, atom2.StartPosition);
            Assert.AreEqual(atom.EndPosition, atom2.EndPosition);
            Assert.AreEqual(atom.Content, atom2.Content);
        }

        [Test]
        public void TestGetIndexInParent()
        {
            ParseTree parseTree = GetTree("(map (lambda (x) (* x x)) '(1 2 3 4 5))");
            ParseTree ptAtom = parseTree.GetNodeAtPath(new CowList<int>(2, 0, 3));
            Assert.IsTrue(ptAtom is ParseTreeAtom);
            ParseTreeAtom atom = ptAtom as ParseTreeAtom;
            Assert.AreEqual(3, atom.GetIndexInParent());
            Assert.AreEqual(-1, parseTree.GetIndexInParent());
        }

        [Test]
        public void TestToString()
        {
            ParseTree parseTree = GetTree("(+ 1 2)");
            Assert.AreEqual("[ParseTree: StartPosition=0, EndPosition=7, SubTrees=3]",
                            parseTree.ToString());
        }

        [Test]
        public void TestPreviousSibling()
        {
            ParseTree parseTree = GetTree("(map (lambda (x) (* x x)) '(1 2 3 4 5))");
            ParseTree ptAtom = parseTree.GetNodeAtPath(new CowList<int>(2, 0, 3));
            Assert.IsTrue(ptAtom is ParseTreeAtom);
            ParseTreeAtom atom = ptAtom as ParseTreeAtom;
            ParseTree ptPrevAtom = atom.GetPreviousSibling();
            Assert.IsTrue(ptPrevAtom is ParseTreeAtom);
            ParseTreeAtom prevAtom = ptPrevAtom as ParseTreeAtom;
            Assert.AreEqual("3", prevAtom.Content);
            Assert.AreEqual(32, prevAtom.StartPosition);
            Assert.AreEqual(33, prevAtom.EndPosition);
            Assert.AreEqual(new CowList<int>(2, 0, 2), prevAtom.GetPath());
        }
      
        [Test]
        public void TestNextSibling()
        {
            ParseTree parseTree = GetTree("(map (lambda (x) (* x x)) '(1 2 3 4 5))");
            ParseTree ptAtom = parseTree.GetNodeAtPath(new CowList<int>(1, 2, 0));
            Assert.IsTrue(ptAtom is ParseTreeAtom);
            ParseTreeAtom atom = ptAtom as ParseTreeAtom;
            ParseTree ptNextAtom = atom.GetNextSibling();
            Assert.IsTrue(ptNextAtom is ParseTreeAtom);
            ParseTreeAtom nextAtom = ptNextAtom as ParseTreeAtom;
            Assert.AreEqual("x", nextAtom.Content);
            Assert.AreEqual(20, nextAtom.StartPosition);
            Assert.AreEqual(21, nextAtom.EndPosition);
            Assert.AreEqual(new CowList<int>(1, 2, 1), nextAtom.GetPath());
        }

        [Test]
        public void TestMoveUp()
        {
            ParseTree parseTree = GetTree(@"
(map (lambda (x) (* x x)) 
     '(1 2 3 4 5))");
            ParseTree quotedList = parseTree.GetNodeAtPath(new CowList<int>(2));
            Assert.IsTrue(quotedList is ParseTreeQuote);
            parseTree = quotedList.MoveUp();
            // The first newline is missing in 'expectedCode' below because
            // it is not part of the (map ...) node in the source above,
            // therefore it will not be rendered when rendering the (map ...)
            // node as text.
            string expectedCode = @"(map '(1 2 3 4 5) 
     (lambda (x) (* x x)))";
            Assert.AreEqual(expectedCode, parseTree.ToStringAsCode(false));
        }
        
        [Test]
        public void TestMoveDown()
        {
            ParseTree parseTree = GetTree(@"
(map (lambda (x) (* x x)) 
     '(1 2 3 4 5))");
            ParseTree mapAtom = parseTree.GetNodeAtPath(new CowList<int>(0));
            parseTree = mapAtom.MoveDown();
            string expectedCode = @"((lambda (x) (* x x)) map 
     '(1 2 3 4 5))";
            Assert.AreEqual(expectedCode, parseTree.ToStringAsCode(false));
        }

        [Test]
        public void TestInsertChar()
        {
            string sample = "()";
            TextWithChanges text = new TextWithChanges();
            text.SetText(sample);
            CowList<Token> tokens = GetTokens(sample);
            CowList<Token> remainingTokens;
            ParseTree parseTree;
            SexpParser.GetInstance().ParserFunc(tokens, text, out parseTree, out remainingTokens);
            Assert.AreEqual(0, remainingTokens.Count);
            Assert.AreEqual("()", parseTree.ToStringAsCode(false));
            parseTree = parseTree.CharInsertedAt('a', 1);
            parseTree = parseTree.CharInsertedAt(' ', 2);
            parseTree = parseTree.CharInsertedAt('b', 3);
            Assert.AreEqual("(a b)", parseTree.ToStringAsCode(false));
            parseTree = parseTree.CharInsertedAt('\'', 3);
            Assert.AreEqual("(a 'b)", parseTree.ToStringAsCode(false));
            parseTree = parseTree.CharInsertedAt(')', 4);
            parseTree = parseTree.CharInsertedAt(')', 5);
            Assert.AreEqual("(a '))b)", parseTree.ToStringAsCode(false));
            Assert.AreEqual("(a 'b)", parseTree.ToStringAsCode(true));
            Assert.AreEqual("_(_a_ _'A)A)_b_)", text.TestRender());
            parseTree = parseTree.CharInsertedAt('(', 4);
            parseTree = parseTree.CharInsertedAt(' ', 6);
            Assert.AreEqual("(a '() )b)", parseTree.ToStringAsCode(false));
            Assert.AreEqual("(a 'b)", parseTree.ToStringAsCode(true));
            Assert.AreEqual("_(_a_ _'A(A)A A)_b_)", text.TestRender());
            CowList<ParseTree> pathToBTree = parseTree.GetPathForPosition(6);
            ParseTree quotedTree = pathToBTree[1];
            parseTree = quotedTree.MoveUp();
            Assert.AreEqual("('() )b a)", parseTree.ToStringAsCode(false));
            Assert.AreEqual("('b a)", parseTree.ToStringAsCode(true));
            Assert.AreEqual("_(_'A(A)A A)_b_ _a_)", text.TestRender());
            parseTree = parseTree.CharInsertedAt(' ', 6);
            parseTree = parseTree.CharInsertedAt('(', 5);
            Assert.AreEqual("('() () b a)", parseTree.ToStringAsCode(false));
            Assert.AreEqual("('() () b a)", parseTree.ToStringAsCode(true));
            Assert.AreEqual("_(_'_(_)_ _(_)_ _b_ _a_)", text.TestRender());
            // Start with this state in the delete char test.
        }

        [Test]
        public void TestDeleteChar()
        {
            string sample = "('() () b a)";
            TextWithChanges text = new TextWithChanges();
            text.SetText(sample);
            CowList<Token> tokens = GetTokens(sample);
            CowList<Token> remainingTokens;
            ParseTree parseTree;
            SexpParser.GetInstance().ParserFunc(tokens, text, out parseTree, out remainingTokens);
            Assert.AreEqual(0, remainingTokens.Count);
            parseTree = parseTree.CharDeletedAt(1);
            Assert.AreEqual("(() () b a)", parseTree.ToStringAsCode(false));
            parseTree = parseTree.CharDeletedAt(2);
            Assert.AreEqual("(() () b a)", parseTree.ToStringAsCode(true));
            Assert.AreEqual("(( () b a)", parseTree.ToStringAsCode(false));
            Assert.AreEqual("_(_(D)_ _(_)_ _b_ _a_)", text.TestRender());
            parseTree = parseTree.CharDeletedAt(1);
            Assert.AreEqual("( () b a)", parseTree.ToStringAsCode(true));
            Assert.AreEqual("( () b a)", parseTree.ToStringAsCode(false));
            Assert.AreEqual("_(_ _(_)_ _b_ _a_)", text.TestRender());
        }

        // TODO: After associating the TextWithChanges with an IAbstractEditor instance,
        // we need to provide a test implementation of IAbstractEditor, then write tests
        // against it.
        //
        // Alternative: Each ParseTree is associated with the IAbstractEditor instance.
        // This makes more sense as the parse tree controls both text and editor,
        // and is controlled by editor. Anyway, to early to tell what's best.
        //
        // This will provide a framework to write regression tests. The IAbstractEditor
        // implementation will need to be able to simulate a real editor - with navigation,
        // selection, typing text etc. It will be a mock object emulating an interactive
        // editor.

        #region Private Helper Methods
        // The following functions are debugging helpers, they are not
        // used normally.
        #pragma warning disable 0169
        private CowList<Token> GetTokens(string sourceCode)
        {
            return SexpParser.GetInstance().TokenizerFunc(EnumerateString(sourceCode));
        }
        
        private ParseTree GetTree(string sourceCode)
        {
            Synpl.Core.Parser parser = SexpParser.GetInstance();
            CowList<CharWithPosition> text = EnumerateString(sourceCode);
            CowList<Token> tokens = parser.TokenizerFunc(text);
            TextWithChanges textWithChanges = new TextWithChanges();
            textWithChanges.SetText(sourceCode);
            ParseTree parseTree;
            CowList<Token> remainingTokens;
            parser.ParserFunc(tokens, textWithChanges, out parseTree, out remainingTokens);
            Assert.AreEqual(0, remainingTokens.Count);
            return parseTree;
        }
        
        private string DumpCollection<T>(IList<T> collection)
        {
            StringBuilder sb = new StringBuilder();
            foreach (T item in collection)
            {
                sb.Append(item.ToString());
                sb.Append("\n");
            }
            return sb.ToString();
        }
        
        private CowList<CharWithPosition> EnumerateString(string str)
        {
            CowList<CharWithPosition> result = new CowList<CharWithPosition>();
            for (int i = 0; i < str.Length; i++)
            {
                result.Add(new CharWithPosition(i, str[i]));
            }
            return result;
        }
        #endregion
    }
}

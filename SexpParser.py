# Synpl - a "structured editor" plugin for Gedit.
# Copyright (C) 2009  Miron Brezuleanu <mbrezu@gmail.com>

# This program is free software; you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation; either version 2 of the License, or
# (at your option) any later version.

# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.

# You should have received a copy of the GNU General Public License along
# with this program; if not, write to the Free Software Foundation, Inc.,
# 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.

from pprint import pprint
from Synpl import ParseTree, Parser, Token, ParseException
from TerminalController import TerminalController
from TextWithChanges import TextWithChanges

class ParseTreeAtom(ParseTree):

    def __init__(self, startPosition, endPosition, content, parser, parent, text):
        super(ParseTreeAtom, self).__init__(startPosition, endPosition, [], parser, parent, text)
        self.content = content

    def __repr__(self):
        return "ParseTreeAtom(%d, %d, \"%s\")" % (self.startPosition,
                                                  self.endPosition,
                                                  self.content)

    def reprAsLabel(self):
        return self.content

class ParseTreeQuote(ParseTree):

    def __init__(self, startPosition, endPosition, quotedTree, parser, parent, text):
        super(ParseTreeQuote, self).__init__(startPosition,
                                             endPosition,
                                             [quotedTree],
                                             parser,
                                             parent,
                                             text)

    def reprAsLabel(self):
        return "' (quote)"

class ParseTreeList(ParseTree):

    def __init__(self, startPosition, endPosition, members, parser, parent, text):
        super(ParseTreeList, self).__init__(startPosition,
                                            endPosition,
                                            members,
                                            parser,
                                            parent,
                                            text)

    def reprAsLabel(self):
        return "() (list)"

def parserSexp(sexpTokens, textWithChanges):
    global parser
    if len(sexpTokens) == 0:
        raise ParseException("No tokens in stream.")
    if sexpTokens[0].kind == "QUOTE":
        quotedTree, tokensRest = parserSexp(sexpTokens[1:], textWithChanges)
        result = ParseTreeQuote(sexpTokens[0].startPosition,
                                quotedTree.endPosition,
                                quotedTree,
                                parser,
                                None,
                                textWithChanges)
        quotedTree.parent = result
        return result, tokensRest
    elif sexpTokens[0].kind == "OPENPAREN":
        members = []
        iterTokens = sexpTokens[1:]
        while len(iterTokens) > 0 and iterTokens[0].kind != "CLOSEPAREN":
            member, iterTokens = parserSexp(iterTokens, textWithChanges)
            members.append(member)
        if len(iterTokens) == 0:
            raise ParseException("No tokens in stream, expected ')'.")
        result = ParseTreeList(sexpTokens[0].startPosition,
                               iterTokens[0].endPosition,
                               members,
                               parser,
                               None,
                               textWithChanges)
        for m in members:
            m.parent = result
        return result, iterTokens[1:]
    elif sexpTokens[0].kind == "CLOSEPAREN":
        raise ParseException("Unexpected ')'.")
    else:
        return ParseTreeAtom(sexpTokens[0].startPosition,
                             sexpTokens[0].endPosition,
                             sexpTokens[0].content,
                             parser,
                             None,
                             textWithChanges),\
               sexpTokens[1:]

def tokenizerSexp(text):
    result = []
    pos = 0
    while pos < len(text):
        if text[pos][1] == "(":
            result.append(Token("(", "OPENPAREN", text[pos][0], text[pos][0]))
            pos += 1
        elif text[pos][1] == ")":
            result.append(Token(")", "CLOSEPAREN", text[pos][0], text[pos][0]))
            pos += 1
        elif text[pos][1].isspace():
            while pos < len(text) and text[pos][1].isspace():
                pos += 1
        elif text[pos][1] == "'":
            result.append(Token("'", "QUOTE", text[pos][0], text[pos][0]))
            pos += 1
        elif text[pos][1] == ";":
            while pos < len(text) and text[pos][1] != "\n":
                pos += 1
        else:
            atom = ""
            startPos = text[pos][0]
            while pos < len(text) and not text[pos][1].isspace() and text[pos][1] not in "()'":
                atom += text[pos][1]
                endPos = text[pos][0]
                pos += 1
            result.append(Token(atom, "ATOM", startPos, endPos))
    return result

currentId = 0
def getNextId():
    global currentId
    currentId += 1
    return currentId

parser = Parser(parserSexp, tokenizerSexp, getNextId)

def removePositions(code):
    return "".join(apply(zip, code)[1])

def test(sample):
    text = TextWithChanges()
    text.text = sample
    tokens = tokenizerSexp(text.getOldSlice(0, text.getActualLength() - 1))
    lastAtomPosition = 0
    for token in tokens:
        if token.kind == "ATOM":
            lastAtomPosition = token.startPosition
    print "Last atom position is", lastAtomPosition
#    pprint(tokens)
    tree, tokensRest = parserSexp(tokens, text)
#    pprint(tree)
#    pprint(tokensRest)
    pathToLastAtom = tree.getPathForPosition(lastAtomPosition)
    for t in pathToLastAtom:
        dumpTree(">> ", t)
    lastAtom = pathToLastAtom[-1]
    print "Last atom is", lastAtom
    prevAtom = lastAtom.getPreviousSibling()
    print "Previous atom is", prevAtom
    qList = pathToLastAtom[-3]
    print "Quoted list is", removePositions(qList.reprAsCode())
    lam = qList.getPreviousSibling()
    print "Lambda is", removePositions(lam.reprAsCode())
    qListPath = qList.getPath()
    print "Path to lambda is", lam.getPath()
    print "Path to quoted list is", qListPath
    print "Path to last atom is ", lastAtom.getPath()
    # this invalidates pathToLastAtom, lastAtom, prevAtom, qList and lam
    newTree = qList.moveUp()
    qListPath[-1] = qListPath[-1] - 1
    print "New path to quoted list is", qListPath
    qList = newTree.getNodeAtPath(qListPath)
    print "Quoted list is", removePositions(qList.reprAsCode())
    print removePositions(newTree.reprAsCode())
    newTree = qList.moveDown()
    print removePositions(newTree.reprAsCode())

def dumpTree(prompt, tree):
    print "*" * 50
    print prompt
#    print tree.reprAsCode(useOldVersion=True)
#    print tree.reprAsCode(useOldVersion=False)
    print tree.reprAsTree()

def testInsertChar():
    sample = "()"
    text = TextWithChanges()
    text.text = sample
    tokens = tokenizerSexp(text.getOldSlice(0, text.getActualLength() - 1))
    def insertChar(tree, char, pos):
        tree = tree.charInsertedAt(char, 1)
        return tree
    def treeAndText(prompt, tree):
        dumpTree(prompt, tree)
        term = TerminalController()
        tree.text.colorRender(term)
#    print tokens
    tree, tokensRest = parserSexp(tokens, text)
    treeAndText("initial tree is", tree)
    tree = tree.charInsertedAt('a', 1)
    tree = tree.charInsertedAt(' ', 2)
    tree = tree.charInsertedAt('b', 3)
    treeAndText("rewritten tree is", tree)
    tree = tree.charInsertedAt("'", 3)
    treeAndText("rewritten tree is", tree)
    tree = tree.charInsertedAt(')', 4)
    tree = tree.charInsertedAt(')', 5)
    treeAndText("rewritten tree is", tree)

    tree = tree.charInsertedAt('(', 4)
#     tree = tree.charInsertedAt(' ', 6)
#     treeAndText("fixed tree is", tree)
#     tree = tree.charInsertedAt(' ', 9)
    treeAndText("fixed tree is", tree)

    pathToBTree = tree.getPathForPosition(6)
    for node in pathToBTree:
        print ">>>"
        print node.reprAsTree()
    qTree = pathToBTree[-2]

    tree = qTree.moveUp()
    treeAndText("tree after move is", tree)
    print qTree.getPath()

    tree = tree.charInsertedAt('(', 4)
    treeAndText("fixed tree is", tree)

    tree = tree.charDeletedAt(1)
    treeAndText("deleted quote", tree)

    tree = tree.charDeletedAt(2)
    treeAndText("deleted close paren", tree)

    tree = tree.charDeletedAt(1)
    treeAndText("deleted open paren", tree)

sample = """
(define (somefunc u t)
  (map (lambda (x) (* x x)) ;; this is a comment
       '(1 2 3)))"""

sample2 = "(+ (* 4 3) '2)"

sample3 = """
(map (lambda (x) (* x x))
     '(1 2 3))"""

sample4 = """
(map (lambda (x) (* x x)) ;; this is a comment
     '(1 2 3)))"""

sample5 = ""

sample6 = "()"

#test(sample3)
testInsertChar()

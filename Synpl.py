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

# TODO:
# - write documentation (yeah, right)
# - break code in modules
# - support for deleted extra chars
# - unit tests!!!!

from pprint import pprint
from TextWithChanges import TextWithChanges
from TerminalController import TerminalController

currentId = 0
def getNextId():
    global currentId
    currentId += 1
    return currentId

class Token(object):
    def __init__(self, content, kind, startPosition, endPosition):
        self.startPosition = startPosition
        self.endPosition = endPosition
        self.content = content
        self.kind = kind

    def __repr__(self):
        return "Token(%d, %d, %s, \"%s\")" % (self.startPosition,
                                              self.endPosition,
                                              repr(self.content),
                                              self.kind)

    def offsetBy(self, positionOffset):
        self.startPosition += positionOffset
        self.endPosition += positionOffset

class ParseException(Exception):
    pass

# parse functions signature
# INPUT:
# - tokens: [Token], list of tokens
# - text: TextWithChanges, the representation of the code we're parsing
# OUTPUT:
# - tree: parseTree, the parse tree representing the code
# - tokensRest: [Token], list of unused tokens

# tokenizer functions signature
# INPUT:
# - code: string, code to tokenize
# - offset: int, an offset specifying the position of 'code' in the larger body of code
# OUTPUT:
# - tokens: [Token], list of tokens, can be passed to a parser

# the id generator is responsible for generating unique ids on each call. The
# ids must be comparable for equality (ints, strings are OK).
class Parser(object):
    def __init__(self, parser, tokenizer, idGenerator):
        self.parser = parser
        self.tokenizer = tokenizer
        self.idGenerator = idGenerator

class ParseTree(object):

    def __init__(self, startPosition, endPosition, subTrees, parser, parent, text):
        self.startPosition = startPosition
        self.endPosition = endPosition
        self.subTrees = subTrees
        self.parser = parser
        self.parent = parent
        self.id = self.parser.idGenerator()
        self.text = text

    def reprAsLabel(self):
        raise NotImplementedError()

    def reprAsTree(self, indent = 0):
        resultLines = []
        resultLines.append("%s(%d,%d): %s" % (" " * indent,
                                                 self.startPosition,
                                                 self.endPosition,
                                                 self.reprAsLabel()))
        for node in self.subTrees:
            resultLines.append(node.reprAsTree(indent + 2))
        return "\n".join(resultLines)

    def __repr__(self):
        return "%s(%d, %d, [%s], \"%s\")" % (self.__class__.__name__,
                                                       self.startPosition,
                                                       self.endPosition,
                                                       ", ".join([repr(m) for m in self.subTrees]),
                                                       self.parser.parser.__name__)
    def getPath(self):
        index = self.getIndexInParent()
        if index == -1:
            return []
        else:
            return self.parent.getPath() + [index]

    def getNodeAtPath(self, path):
        if len(path) == 0:
            return self
        else:
            head = path[0]
            if head > 0 and head < len(self.subTrees):
                return self.subTrees[head].getNodeAtPath(path[1:])
            else:
                return None

    def contains(self, position):
        return position >= self.startPosition and position <= self.endPosition

    def getPathForPosition(self, position):
        if not self.contains(position):
            return []
        subTreeIter = self.subTrees
        while len(subTreeIter) > 0:
            head = subTreeIter[0]
            subTreeIter = subTreeIter[1:]
            if position < head.startPosition:
                return [self]
            if head.contains(position):
                return [self] + head.getPathForPosition(position)
        return [self]

    def offsetBy(self, offset):
        self.startPosition += offset
        self.endPosition += offset
        for node in self.subTrees:
            node.offsetBy(offset)

    def offsetSuccessorsBy(self, offset):
        index = self.getIndexInParent()
        if index == -1:
            return
        i = index + 1
        while i < len(self.parent.subTrees):
            self.parent.subTrees[i].offsetBy(offset)
            i += 1
        self.parent.endPosition += offset
        self.parent.offsetSuccessorsBy(offset)

    def getIndexInParent(self):
        if self.parent == None:
            return -1
        siblings = self.parent.subTrees
        for index, sibling in enumerate(siblings):
            if sibling.id == self.id:
                return index
        return -1

    def getParentNumberOfChildren(self):
        if self.parent == None:
            return 0
        return len(self.parent.subTrees)

    def getPreviousSibling(self):
        index = self.getIndexInParent()
        if index < 1:
            return None
        return self.parent.subTrees[index - 1]

    def getNextSibling(self):
        index = self.getIndexInParent()
        if index == -1 or index == len(self.parent.subTrees) - 1:
            return None
        return self.parent.subTrees[index + 1]

    def moveUp(self):
        previous = self.getPreviousSibling()
        if previous == None:
            return None

#         print ">>>"
#         print removePositions(self.reprAsCode())
#         print removePositions(previous.reprAsCode())
#         print removePositions(self.getRoot().reprAsCode())
        thisTwcSlice = self.text.getSliceWithChanges(self.startPosition,
                                                     self.endPosition)
        self.text.removeSliceWithChanges(self.startPosition,
                                         self.endPosition)
        previousTwcSlice = self.text.getSliceWithChanges(previous.startPosition,
                                                         previous.endPosition)
        self.text.removeSliceWithChanges(previous.startPosition,
                                         previous.endPosition)

#         print removePositions(self.getRoot().reprAsCode())
        self.text.insertSliceWithChanges(previous.startPosition, thisTwcSlice)
#         print removePositions(self.getRoot().reprAsCode())
        adjustedSelfStartPosition = self.startPosition + \
                                    thisTwcSlice.getActualLength() - \
                                    previousTwcSlice.getActualLength()
        self.text.insertSliceWithChanges(adjustedSelfStartPosition, previousTwcSlice)
#         print removePositions(self.getRoot().reprAsCode())
#         print "<<<"

        # Use old version because we don't want extra unparsed
        # characters in any of the parent's children (including the
        # two being swapped) to break the parse.
        reparsedRoot = self.parent._tryReparse(useOldVersion = True)

        if reparsedRoot != None:
            return reparsedRoot
        else:
            # If the reparse is not successful, we need to swap back
            # the nodes - moving up or down should not break the parse.
            #
            # CAUTION: this piece of code is not tested with the sexp
            # parser, because swapping elements in a list is always
            # parsable. We need some kind of MiniPascal or similar to
            # test it.
            self.text.removeSliceWithChanges(
                previous.startPosition,
                previous.startPosition + thisTwcSlice.getActualLength() - 1)
            self.text.removeSliceWithChanges(
                adjustedSelfStartPosition,
                self.startPosition + previousTwcSlice.getActualLength() - 1)
            self.text.insertSliceWithChanges(previous.startPosition, previousTwcSlice)
            self.text.insertSliceWithChanges(self.startPosition, thisTwcSlice)
            return None

    def moveDown(self):
        next = self.getNextSibling()
        if next != None:
            return next.moveUp()
        else:
            return None

    def duplicate(self):
        index = self.getIndexInParent()
        if index == -1:
            return None
        self.parent.subTrees.insert(index + 1, self)
        result = self.parent._reparse()
        # decouple self from tree structure to prevent hard to fix bugs
        self.parent = None
        return result

    def reprAsCode(self, useOldVersion = True):
        if useOldVersion:
            result = self.text.getOldSlice(self.startPosition, self.endPosition)
        else:
            result = self.text.getCurrentSlice(self.startPosition, self.endPosition)
        return result

    # returns a root node if the code is parsable, None otherwise
    # basically a wrapper for reparse
    def _tryReparse(self, useOldVersion = True):
        try:
            result = self._reparse(useOldVersion)
        except ParseException, e:
            return None
        return result

    # since reparses can affect the tree (it replaces itself in the
    # parent) and we may be the root, we return the root of the tree
    # so that the caller can update its data. All external references
    # to 'self' become invalid after calling this function (the node
    # is replaced in the parse tree)
    def _reparse(self, useOldVersion = True):
        code = self.reprAsCode(useOldVersion)
        tokens = self.parser.tokenizer(code)
        reparsedSelf, tokensRest = self.parser.parser(tokens, self.text)
        if len(tokensRest) > 0:
            raise ParseException("Extra tokens in stream.")
        index = self.getIndexInParent()
        if index != -1:
            self.parent.subTrees[index] = reparsedSelf
            reparsedSelf.parent = self.parent
            self.parent = None
        reparsedSelf._adjustCoordinates(self)
        return reparsedSelf.getRoot()

    def _adjustCoordinates(self, oldNode):
        offset = self.endPosition - oldNode.endPosition
        if offset != 0:
            self.offsetSuccessorsBy(offset)

    def getRoot(self):
        result = self
        while result.parent != None:
            result = result.parent
        return result

    def charInsertedAt(self, char, position):
        path = self.getPathForPosition(position)
        if len(path) == 0:
            nodeAffected = self
        else:
            nodeAffected = path[-1]
        return nodeAffected._charInsertedAtImpl(position, char)

    def _charInsertedAtImpl(self, position, char):
        self.text.insertChar(char, position)
        self.endPosition += 1
        self.offsetSuccessorsBy(1)
        def reparse(node):
            reparsedNode = node._tryReparse(useOldVersion = False)
            if reparsedNode != None:
                reparsedNode._adjustCoordinates(node)
                reparsedNode.text.validateSlice(reparsedNode.startPosition,
                                                reparsedNode.endPosition)
                return reparsedNode.getRoot()
            else:
                if node.parent != None:
                    return reparse(node.parent)
                else:
                    return node.getRoot()
        return reparse(self)

    def charDeletedAt(self, position, char):
        raise NotImplementedError()

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
#    print prompt, tree
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
#     tree = tree.charInsertedAt('(', 7)
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

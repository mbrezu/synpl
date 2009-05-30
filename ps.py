
# TODO:
# - write documentation (yeah, right)
# - break code in modules
# - support for deleted extra chars
# - unit tests!!!!

from pprint import pprint

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

    def __init__(self, startPosition, endPosition, subTrees, parser, parent):
        self.startPosition = startPosition
        self.endPosition = endPosition
        self.subTrees = subTrees
        self.parser = parser
        self.parent = parent
        self.id = self.parser.idGenerator()
        self.extraChars = ExtraCharCollection(self)

    def reprAsLabel(self):
        raise NotImplementedError()

    def reprAsTree(self, indent = 0, withExtraChars = True):
        resultLines = []
        if withExtraChars and not self.extraChars.isEmpty():
            extraChars = self.extraChars.reprAsLabel()
        else:
            extraChars = ""
        resultLines.append("%s(%d,%d): %s%s" % (" " * indent,
                                                 self.startPosition,
                                                 self.endPosition,
                                                 self.reprAsLabel(),
                                                 extraChars))
        for node in self.subTrees:
            resultLines.append(node.reprAsTree(indent + 2, withExtraChars = withExtraChars))
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

    def moveUp(self, editorText):
        # TODO: broken in the presence of extra chars, we need to use
        # reprAsCode to retrieve the code for self and previous then
        # strip the whitespace from the code (maybe a function
        # reprAsCodeWithoutWhiteSpace or stripWhiteSpaceFromReprCode -
        # latter is better)
        #
        # also, after a successful swap, we must also swap the extra
        # characters (the swap should leave them untouched).
        #
        # they don't need to be re-adjusted because they have relative
        # positions
        return self.getRoot()

    def moveDown(self, editorText):
        next = self.getNextSibling()
        if next != None:
            return next.moveUp(editorText)
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

    def reprAsCode(self, withExtraChars = False):
        result = self._reprAsCodeImpl(withExtraChars)
        if withExtraChars:
            result = self.extraChars.addToCode(result)
        return result

    def _reprAsCodeImpl(self, withExtraChars = False):
        raise NotImplementedError()

    # returns a root node if the code is parsable, None otherwise
    # basically a wrapper for reparse
    def _tryReparse(self, code = None, addExtraChars = False):
        try:
            result = self._reparse(code, addExtraChars)
        except ParseException, e:
            return None
        return result

    # we use this function to access the tokens
    # because it adjusts for deleted and added characters
    def _getTokens(self, code = None, adjustExtraChars = True):
        def basicGetTokens(code = None):
            if self.parser == None:
                raise ParseException("No parser.")
            offset = self.startPosition
            if code == None:
                code = self.reprAsCode()
            return self.parser.tokenizer(code, offset)
        tokens = basicGetTokens(code)
        # adjust for extra characters, but don't include them in tokens
        if adjustExtraChars:
            offset = self.startPosition
            return self.extraChars.adjustTokensPositions(tokens, offset)
        else:
            return tokens

    # since reparses can affect the tree (it replaces itself in the
    # parent) and we may be the root, we return the root of the tree
    # so that the caller can update its data. All external references
    # to 'self' become invalid after calling this function (the node
    # is replaced in the parse tree)
    #
    # addExtraChars == True means we want to include extra chars (stored because
    # they broke a previous parse) in this parse
    def _reparse(self, code = None, addExtraChars = False):
        if code == None:
            code = self.reprAsCode(withExtraChars = addExtraChars)
        adjustExtraChars = not addExtraChars
        tokens = self._getTokens(code, adjustExtraChars)
        reparsedSelf, tokensRest = self.parser.parser(tokens)
        if len(tokensRest) > 0:
            raise ParseException("Extra tokens in stream.")
        index = self.getIndexInParent()
        if index != -1:
            self.parent.subTrees[index] = reparsedSelf
            reparsedSelf.parent = self.parent
            self.parent = None
        reparsedSelf._adjustCoordinates(self)
        return reparsedSelf.getRoot()

    def _adjustCoordinates(self, old):
        offset = self.endPosition - old.endPosition
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
        print "inserting", char
#         dumpTree("before insert", self)
#         dumpTree("root before insert", self.getRoot())
        self.extraChars.addExtraChar(char, position - self.startPosition, False)
        dumpTree("before parse", self)
        def reparse(node, level):
            if level == 0:
                return node.getRoot()
            else:
                reparsedNode = node._tryReparse(addExtraChars = True)
                if reparsedNode != None:
                    reparsedNode._adjustCoordinates(node)
                    return reparsedNode.getRoot()
                else:
                    if node.parent != None:
                        return reparse(node.parent, level - 1)
                    else:
                        return node.getRoot()
        return reparse(self, 2)

    def charDeletedAt(self, position, char):
        raise NotImplementedError()

class ParseTreeAtom(ParseTree):

    def __init__(self, startPosition, endPosition, content, parser, parent):
        super(ParseTreeAtom, self).__init__(startPosition, endPosition, [], parser, parent)
        self.content = content

    def __repr__(self):
        return "ParseTreeAtom(%d, %d, \"%s\")" % (self.startPosition,
                                                  self.endPosition,
                                                  self.content)

    def _reprAsCodeImpl(self, withExtraChars = False):
        return self.content

    def reprAsLabel(self):
        return self.content

class ParseTreeQuote(ParseTree):

    def __init__(self, startPosition, endPosition, quotedTree, parser, parent):
        super(ParseTreeQuote, self).__init__(startPosition,
                                             endPosition,
                                             [quotedTree],
                                             parser,
                                             parent)

    def _reprAsCodeImpl(self, withExtraChars = False):
        return "'" + self.subTrees[0].reprAsCode(withExtraChars)

    def reprAsLabel(self):
        return "' (quote)"

class ParseTreeList(ParseTree):

    def __init__(self, startPosition, endPosition, members, parser, parent):
        super(ParseTreeList, self).__init__(startPosition,
                                            endPosition,
                                            members,
                                            parser,
                                            parent)

    def _reprAsCodeImpl(self, withExtraChars):
        members = [m.reprAsCode(withExtraChars) for m in self.subTrees]
        return "(" + "".join(members) + ")"

    def reprAsLabel(self):
        return "() (list)"

def parserSexp(sexpTokens):
    global parser
    if len(sexpTokens) == 0:
        raise ParseException("No tokens in stream.")
    if sexpTokens[0].kind == "QUOTE":
        quotedTree, tokensRest = parserSexp(sexpTokens[1:])
        result = ParseTreeQuote(sexpTokens[0].startPosition,
                                quotedTree.endPosition,
                                quotedTree,
                                parser,
                                None)
        result, tokensRest2 = addWsAfterToNode(result, tokensRest)
        quotedTree.parent = result
        return result, tokensRest2
    elif sexpTokens[0].kind == "OPENPAREN":
        members = []
        iterTokens = sexpTokens[1:]
        while len(iterTokens) > 0 and iterTokens[0].kind != "CLOSEPAREN":
            member, iterTokens = parserSexp(iterTokens)
            members.append(member)
        if len(iterTokens) == 0:
            raise ParseException("No tokens in stream, expected ')'.")
        result = ParseTreeList(sexpTokens[0].startPosition,
                               iterTokens[0].endPosition,
                               members,
                               parser,
                               None)
        for m in members:
            m.parent = result
        return result, iterTokens[1:]
    elif sexpTokens[0].kind == "CLOSEPAREN":
        raise ParseException("Unexpected ')'.")
    elif sexpTokens[0].kind in ["WHITESPACE", "COMMENT"]:
        whiteSpace = []
        origSexpTokens = sexpTokens
        while len(sexpTokens) > 0 and sexpTokens[0].kind in ["WHITESPACE", "COMMENT"]:
            whiteSpace.append(sexpTokens[0])
            sexpTokens = sexpTokens[1:]
        if len(sexpTokens) == 0:
            raise ParseException("No tokens in stream.")
        tree, tokensRest = parserSexp(sexpTokens)
        tree.whiteSpaceBefore = whiteSpace
        return tree, tokensRest
    else:
        return ParseTreeAtom(sexpTokens[0].startPosition,
                             sexpTokens[0].endPosition,
                             sexpTokens[0].content,
                             parser,
                             None),\
               sexpTokens[1:]

def tokenizerSexp(text, offset):
    result = []
    pos = 0
    while pos < len(text):
        if text[pos] == "(":
            result.append(Token("(", "OPENPAREN", pos, pos))
            pos += 1
        elif text[pos] == ")":
            result.append(Token(")", "CLOSEPAREN", pos, pos))
            pos += 1
        elif text[pos].isspace():
            whiteSpace = ""
            startPos = pos
            while pos < len(text) and text[pos].isspace():
                whiteSpace += text[pos]
                pos += 1
            endPos = pos - 1
        elif text[pos] == "'":
            result.append(Token("'", "QUOTE", pos, pos))
            pos += 1
        elif text[pos] == ";":
            comment = ""
            startPos = pos
            while pos < len(text) and text[pos] != "\n":
                comment += text[pos]
                pos += 1
            endPos = pos - 1
        else:
            atom = ""
            startPos = pos
            while pos < len(text) and not text[pos].isspace() and text[pos] not in "()'":
                atom += text[pos]
                pos += 1
            endPos = pos - 1
            if endPos < startPos:
                raise ParseException("Empty atom.")
            result.append(Token(atom, "ATOM", startPos, endPos))
    for token in result:
        token.offsetBy(offset)
    return result

parser = Parser(parserSexp, tokenizerSexp, getNextId)

def test(sample):
    tokens = tokenizerSexp(sample, 0)
    lastAtomPosition = 0
    for token in tokens:
        if token.kind == "ATOM":
            lastAtomPosition = token.startPosition
    print "Last atom position is", lastAtomPosition
    pprint(tokens)
    tree, tokensRest = parserSexp(tokens)
#    pprint(tree)
#    pprint(tokensRest)
    asString = tree.reprAsCode()
    print sample
    print asString
    print sample == asString
    print "gq"
    pathToLastAtom = tree.getPathForPosition(lastAtomPosition)
    pprint([t.shortRepr() for t in pathToLastAtom])
    lastAtom = pathToLastAtom[-1]
    print "Last atom is", lastAtom
    prevAtom = lastAtom.getPreviousSibling()
    print "Previous atom is", prevAtom
    qList = pathToLastAtom[-3]
    print "Quoted list is", qList.reprAsCode()
    lam = qList.getPreviousSibling()
    print "Lambda is", lam.reprAsCode()
    qListPath = qList.getPath()
    print "Path to lambda is", lam.getPath()
    print "Path to quoted list is", qListPath
    print "Path to last atom is ", lastAtom.getPath()
    # this invalidates pathToLastAtom, lastAtom, prevAtom, qList and lam
    newTree = qList.moveUp()
    qListPath[-1] = qListPath[-1] - 1
    print "New path to quoted list is", qListPath
    qList = newTree.getNodeAtPath(qListPath)
    print "Quoted list is", qList.reprAsCode()
    print newTree.reprAsCode()
    newTree = qList.moveDown()
    print newTree.reprAsCode()

def testExtraChars():
    l = []
    l.append(ExtraChar('a', 12))
    l.append(ExtraChar('b', 3))
    l.append(ExtraChar('c', 45))
    l.append(ExtraChar('d', 20))
    l.append(ExtraChar('e', 10))
    print l
    l.sort()
    print l

def dumpTree(prompt, tree):
    print "*" * 50
    print prompt, tree
    print tree.reprAsCode()
    print tree.reprAsCode(withExtraChars=True)
    print tree.reprAsTree()

def testInsertChar():
    text = "()"
    def insertChar(tree, char, pos, text):
        tree = tree.charInsertedAt(char, 1)
        text = text[:pos] + char + text[pos:]
        return tree, text
    def treeAndText(prompt, tree, text):
        dumpTree(prompt, tree)
        print prompt, repr(text)
    tokens = tokenizerSexp(text, 0)
#    print tokens
    tree, tokensRest = parserSexp(tokens)
    treeAndText("initial tree is", tree, text)
    tree, text = insertChar(tree, 'a', 1, text)
    tree, text = insertChar(tree, ' ', 2, text)
    tree, text = insertChar(tree, 'b', 3, text)
#     tree, text = insertChar(tree, "'", 3, text)
#     tree, text = insertChar(tree, ')', 4, text)
#     tree, text = insertChar(tree, ')', 4, text)
    treeAndText("rewritten tree is", tree, text)

#     tree, text = insertChar(tree, '(', 4, text)
#     tree, text = insertChar(tree, '(', 5, text)
#     tree, text = insertChar(tree, ' ', 5, text)
#    tree = tree.charInsertedAt(' ', 7)
#    treeAndText("fixed tree is", tree, text)

    # TODO: next lines will work after moveUp will be able to handle
    # extra chars
#    tree = bTree.moveUp()
#    print "tree after move is", tree

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

#test(sample4)
#testExtraChars()
testInsertChar()

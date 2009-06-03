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
# - support for deleted extra chars
# - unit tests!!!!

from TextWithChanges import TextWithChanges

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

    # text is a TextWithChanges instance
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
        nodeAffected.text.insertChar(char, position)
        nodeAffected.endPosition += 1
        nodeAffected.offsetSuccessorsBy(1)
        return nodeAffected._reparseAndValidateRecursively()

    # The first node that doesn't parse after a delete should be
    # marked with squigglies or similar marks to show that something
    # is missing there and the editor parser doesn't use the visible
    # text.
    def charDeletedAt(self, position):
        path = self.getPathForPosition(position)
        if len(path) == 0:
            nodeAffected = self
        else:
            nodeAffected = path[-1]
        nodeAffected.text.deleteChar(position)
        nodeAffected.endPosition -= 1
        nodeAffected.offsetSuccessorsBy(-1)
        return nodeAffected._reparseAndValidateRecursively()

    def _reparseAndValidateRecursively(self):
        reparsedNode = self._tryReparse(useOldVersion = False)
        if reparsedNode != None:
            reparsedNode._adjustCoordinates(self)
            reparsedNode.text.validateSlice(reparsedNode.startPosition,
                                            reparsedNode.endPosition)
            return reparsedNode.getRoot()
        else:
            if self.parent != None:
                return self.parent._reparseAndValidateRecursively()
            else:
                return self.getRoot()


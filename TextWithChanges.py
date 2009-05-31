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

import sys
import unittest

from TerminalController import TerminalController

class TextChange(object):

    def __init__(self, position, deleted):
        self.position = position
        self.deleted = deleted

    def __repr__(self):
        return "TextIncrement(position=%d, deleted=%s)" % (self.position, self.deleted)

    def __cmp__(self, other):
        return self.position - other.position

    def moved(self, offset):
        return TextChange(self.position + offset, self.deleted)

    def between(self, start, end):
        return self.position >= start and self.position <= end

# TextWithChanges (TWC) rules:
# 1. All changes are stored in 'old' coordinates.
# 2. All calls receive parameters in 'new' (editor valid) coordinates.
# 3. Deletes over inserts cancel themselves out.

class TextWithChanges(object):
    def __init__(self):
        self.changes = []
        self.text = ""

    def convertActualPositionToOld(self, actualPosition, findEnd=False):
        result = actualPosition
        for change in self.changes:
            if change.position > result:
                break
            elif change.deleted:
                if findEnd or change.position < result:
                    result += 1
        return result

    def insertChar(self, char, position):
        # Get the old position.
        oldPosition = self.convertActualPositionToOld(position)
        self.text = self.text[:oldPosition] + char + self.text[oldPosition:]
        # Move change markers around.
        k = 0
        while k < len(self.changes):
            change = self.changes[k]
            if change.position >= oldPosition:
                change.position += 1
            k += 1
        # Mark as increment.
        self.changes.append(TextChange(oldPosition, deleted=False))
        self.changes.sort()

    def deleteChar(self, position):
        # Get the old position.
        oldPosition = self.convertActualPositionToOld(position, findEnd=True)
        # Check if we're actually deleting an insertion; if so, they
        # will cancel themselves out and all changes afterwards will
        # be pulled back one character.
        insertionCanceled = False
        k = 0
        while k < len(self.changes):
            change = self.changes[k]
            if change.position == oldPosition and not change.deleted:
                self.text = self.text[:oldPosition] + self.text[oldPosition+1:]
                del changes[k]
                insertionCanceled = True
                break
            k += 1
        if insertionCanceled:
            while k < len(self.changes):
                change = self.changes[k]
                change.position -= 1
                k += 1
        else:
            self.changes.append(TextChange(oldPosition, deleted=True))
            self.changes.sort()

    def colorRender(self, term):
        k = 0
        for index, char in enumerate(self.text):
            if k < len(self.changes) and self.changes[k].position == index:
                change = self.changes[k]
                k += 1
                if  change.deleted:
                    formatString = '${BOLD}${BG_RED}%s${NORMAL}'
                else:
                    formatString = '${BOLD}${BG_GREEN}%s${NORMAL}'
            else:
                formatString = '${BOLD}%s${NORMAL}'
            sys.stdout.write(term.render(formatString % (char,)))
        print

    def testRender(self):
        result = []
        k = 0
        for index, char in enumerate(self.text):
            if k < len(self.changes) and self.changes[k].position == index:
                change = self.changes[k]
                k += 1
                if  change.deleted:
                    formatString = 'D%s'
                else:
                    formatString = 'A%s'
            else:
                formatString = '_%s'
            result.append(formatString % (char,))
        return "".join(result)

    def getSliceWithChanges(self, start, end):
        # Get old coordinates.
        oldStart = self.convertActualPositionToOld(start)
        oldEnd = self.convertActualPositionToOld(end, findEnd=True)
        result = TextWithChanges()
        result.changes = [change.moved(-oldStart)
                          for change in self.changes
                          if change.between(oldStart, oldEnd)]
        result.text = self.text[oldStart:oldEnd+1]
        return result

    def removeSliceWithChanges(self, start, end):
        # Get old coordinates.
        oldStart = self.convertActualPositionToOld(start)
        oldEnd = self.convertActualPositionToOld(end, findEnd=True)
        changesBefore = [change
                         for change in self.changes
                         if change.position < oldStart]
        offset = -(oldEnd - oldStart + 1)
        changesAfter = [change.moved(offset)
                        for change in self.changes
                        if change.position > oldEnd]
        self.changes = changesBefore + changesAfter
        self.text = self.text[:oldStart] + self.text[oldEnd+1:]

    def insertSliceWithChanges(self, position, sliceWithChanges):
        oldPosition = self.convertActualPositionToOld(position)
        changesBefore = [change for change in self.changes if change.position < oldPosition]
        changesAfter = [change.moved(len(sliceWithChanges.text))
                        for change in self.changes
                        if change.position >= oldPosition]
        changesBetween = [change.moved(oldPosition) for change in sliceWithChanges.changes]
        self.changes = changesBefore + changesBetween + changesAfter
        self.text = self.text[:oldPosition] + sliceWithChanges.text + self.text[oldPosition:]

    def getCurrentSlice(self, start, end):
        # Get old coordinates.
        oldStart = self.convertActualPositionToOld(start)
        oldEnd = self.convertActualPositionToOld(end, findEnd=True)
        deletes = [change
                   for change in self.changes
                   if change.between(oldStart, oldEnd) and change.deleted]
        oldSlice = self.text[oldStart:oldEnd+1]
        result = []
        k = 0
        runningIndex = start
        for index, char in enumerate(oldSlice):
            if k < len(deletes):
                delete = deletes[k]
                if delete.position == oldStart + index:
                    k += 1
                    continue
            result.append((runningIndex, char))
            runningIndex += 1
        return result

    def getOldSlice(self, start, end):
        # Get old coordinates.
        oldStart = self.convertActualPositionToOld(start)
        oldEnd = self.convertActualPositionToOld(end, findEnd=True)
        changes = [change for change in self.changes if change.between(oldStart, oldEnd)]
        oldSlice = self.text[oldStart:oldEnd+1]
        result = []
        k = 0
        runningIndex = start
        for index, char in enumerate(oldSlice):
            isDeleted = False
            if k < len(changes):
                change = changes[k]
                if change.position == oldStart + index:
                    k += 1
                    if change.deleted:
                        isDeleted = change.deleted
                    else:
                        runningIndex += 1
                        continue
            result.append((runningIndex, char))
            if not isDeleted:
                runningIndex += 1
        return result

    def validateSlice(self, start, end):
        # Get old coordinates.
        oldStart = self.convertActualPositionToOld(start)
        oldEnd = self.convertActualPositionToOld(end, findEnd=True)
        k = 0
        offset = 0
        changesBefore = [change for change in self.changes if change.position < oldStart]
        deletesBetween = [change
                          for change in self.changes
                          if change.between(oldStart, oldEnd) and change.deleted]
        offset = -len(deletesBetween)
        changesAfter = [change.moved(offset)
                        for change in self.changes
                        if change.position > oldEnd]
        self.changes = changesBefore + changesAfter
        runningOffset = 0
        for delete in deletesBetween:
            pos = delete.position + runningOffset
            self.text = self.text[:pos] + self.text[pos+1:]
            runningOffset -= 1

class TestTextWithChanges(unittest.TestCase):

    # TODO: tests use quite liberal slice coordinates. Should test
    # corner cases better.

    def setUp(self):
        self.term = TerminalController()
        self.text = TextWithChanges()
        self.text.text = "Ana re mere."

    def testInsertion(self):
        self.text.insertChar('a', 4)
#        self.text.colorRender(self.term)
#        print self.text.testRender()
        self.assertEqual(self.text.testRender(), "_A_n_a_ Aa_r_e_ _m_e_r_e_.")

    def testDeletion(self):
        self.text.text = "Ana are mere."
        self.text.deleteChar(4)
        self.assertEqual(self.text.testRender(), "_A_n_a_ Da_r_e_ _m_e_r_e_.")

    def setupText(self):
        self.text.text = "Ana are mere."
        self.text.deleteChar(4)
        self.text.deleteChar(4)
        self.text.insertChar('b', 4)
        self.text.insertChar('c', 5)
        #self.text.colorRender(self.term)

    def testGetOldSlice(self):
        self.setupText()
        slice0 = self.text.getOldSlice(0, 20)
        self.assertEqual(slice0,
                         [(0, 'A'), (1, 'n'), (2, 'a'), (3, ' '),
                          (6, 'a'), (6, 'r'), (6, 'e'), (7, ' '),
                          (8, 'm'), (9, 'e'), (10, 'r'), (11, 'e'), (12, '.')])

    def testGetCurrentSlice(self):
        self.setupText()
        slice0 = self.text.getCurrentSlice(0, 20)
        self.assertEqual(slice0,
                         [(0, 'A'), (1, 'n'), (2, 'a'), (3, ' '),
                          (4, 'b'), (5, 'c'), (6, 'e'), (7, ' '),
                          (8, 'm'), (9, 'e'), (10, 'r'), (11, 'e'), (12, '.')])

    def testGetCurrentSlice(self):
        self.setupText()
        slice0 = self.text.getCurrentSlice(0, 20)
        self.assertEqual(slice0,
                         [(0, 'A'), (1, 'n'), (2, 'a'), (3, ' '),
                          (4, 'b'), (5, 'c'), (6, 'e'), (7, ' '),
                          (8, 'm'), (9, 'e'), (10, 'r'), (11, 'e'), (12, '.')])

    def testValidateSlice(self):
        self.setupText()
        self.text.validateSlice(3, 8)
        #self.text.colorRender(self.term)
        #print self.text.testRender()
        self.assertEqual(self.text.testRender(), "_A_n_a_ _b_c_e_ _m_e_r_e_.")

    def testGetSliceWithChanges(self):
        self.setupText()
        slice0 = self.text.getSliceWithChanges(2, 8)
        #slice0.colorRender(self.term)
        #print slice0.testRender()
        self.assertEqual(slice0.testRender(), "_a_ AbAcDaDr_e_ _m")

    def testRemoveSliceWithChanges(self):
        self.setupText()
        self.text.deleteChar(11)
        self.text.removeSliceWithChanges(2, 8)
        #self.text.colorRender(self.term)
        #print self.text.testRender()
        self.assertEqual(self.text.testRender(), "_A_n_e_rDe_.")

    def testInsertSliceWithChanges(self):
        self.setupText()
        self.text.deleteChar(11)
        #self.text.colorRender(self.term)
        #print self.text.testRender()
        self.assertEqual(self.text.testRender(), "_A_n_a_ AbAcDaDr_e_ _m_e_rDe_.")
        slice0 = self.text.getSliceWithChanges(2, 8)
        #slice0.colorRender(self.term)
        #print slice0.testRender()
        self.assertEqual(slice0.testRender(), "_a_ AbAcDaDr_e_ _m")
        self.text.removeSliceWithChanges(2, 8)
        #self.text.colorRender(self.term)
        #print self.text.testRender()
        self.assertEqual(self.text.testRender(), "_A_n_e_rDe_.")
        self.text.insertSliceWithChanges(2, slice0)
        #self.text.colorRender(self.term)
        #print self.text.testRender()
        self.assertEqual(self.text.testRender(), "_A_n_a_ AbAcDaDr_e_ _m_e_rDe_.")

if __name__ == "__main__":
    unittest.main()

# term = TerminalController()
# text = TextWithChanges()
# text.insertChar('a', 0)
# text.insertChar('b', 1)
# text.insertChar('a', 2)
# text.insertChar('c', 3)
# text.colorRender(term)
# text.validateSlice(1, 2)
# text.colorRender(term)
# text.deleteChar(1)
# text.colorRender(term)
# print text.changes
# print text.getCurrentSlice(0, 2)
# print text.getOldSlice(0, 2)
# sliceC = text.getSliceWithChanges(1, 2)
# print ">>> slice ",
# sliceC.colorRender(term)
# text.colorRender(term)
# slice2 = text.getSliceWithChanges(1, 1)
# text.removeSliceWithChanges(1, 1)
# text.colorRender(term)
# slice2.colorRender(term)
# text.insertSliceWithChanges(1, slice2)
# text.colorRender(term)
# text.validateSlice(0, 2)
# text.colorRender(term)
# print text.testRender()

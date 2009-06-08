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

# This file defines a wxPython graphical shell with a Scintilla
# editor. I will use it to test Synpl interactively.

import wx
import wx.stc as stc

class Editor(stc.StyledTextCtrl):
    def __init__(self, parent, ID):
        stc.StyledTextCtrl.__init__(self, parent, ID)
        self.Bind(wx.EVT_WINDOW_DESTROY, self.OnDestroy)

    def OnDestroy(self, evt):
        # This is how the clipboard contents can be preserved after
        # the app has exited.
        wx.TheClipboard.Flush()
        evt.Skip()


class MyFrame(wx.Frame):

    def __init__(self):
        wx.Frame.__init__(self, None, -1, "Synpl Shell", size=(800, 600))
        self.editor = Editor(self, -1)
        menuBar = wx.MenuBar()
        menuFile = wx.Menu()
        menuBar.Append(menuFile, "&File")
        miExit = menuFile.Append(wx.NewId(), "E&xit", "Exit application.")
        self.Bind(wx.EVT_MENU, self.OnExit, miExit)
        menuEdit = wx.Menu()
        menuBar.Append(menuEdit, "&Edit")
        menuEdit.Append(wx.NewId(), "&Copy", "Copy to clipboard.")
        menuEdit.Append(wx.NewId(), "C&ut", "Cut to clipboard.")
        menuEdit.Append(wx.NewId(), "Paste", "Paste from clipboard.")
        self.SetMenuBar(menuBar)

    def OnExit(self, event):
        self.Close()

if __name__ == "__main__":
    app = wx.PySimpleApp()
    frame = MyFrame()
    frame.Show(True)
    app.MainLoop()


﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using ICSharpCode.SharpDevelop.Editor.CodeCompletion;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ICSharpCode.Core;
using ICSharpCode.Core.WinForms;
using ICSharpCode.SharpDevelop.DefaultEditor.Actions;
using ICSharpCode.SharpDevelop.Editor;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Internal.Templates;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Actions;
using ICSharpCode.TextEditor.Document;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using ICSharpCode.TextEditor.Gui.InsightWindow;

namespace ICSharpCode.SharpDevelop.DefaultEditor.Gui.Editor
{
	public class SharpDevelopTextAreaControl : TextEditorControl
	{
		protected string contextMenuPath = "/SharpDevelop/ViewContent/DefaultTextEditor/ContextMenu";
		const string editActionsPath         = "/AddIns/DefaultTextEditor/EditActions";
		const string formatingStrategyPath   = "/AddIns/DefaultTextEditor/Formatter";
		const string advancedHighlighterPath = "/AddIns/DefaultTextEditor/AdvancedHighlighter";
		
		readonly TextEditorAdapter adapter;
		
		public TextEditorAdapter Adapter {
			get { return adapter; }
		}
		
		ErrorDrawer errorDrawer;
		IAdvancedHighlighter advancedHighlighter;
		
		public SharpDevelopTextAreaControl()
			: this(true, true)
		{
			GenerateEditActions();
			
			TextEditorProperties = SharpDevelopTextEditorProperties.Instance;
		}
		
		protected SharpDevelopTextAreaControl(bool enableFolding, bool sdBookmarks)
		{
			adapter = new TextEditorAdapter(this);
			
			Document.FoldingManager.FoldingStrategy = new ICSharpCode.SharpDevelop.DefaultEditor.Gui.Editor.ParserFoldingStrategy();
//			Document.BookmarkManager.Factory = new Bookmarks.SDBookmarkFactory(Document.BookmarkManager);
//			Document.BookmarkManager.Added   += new BookmarkEventHandler(BookmarkAdded);
//			Document.BookmarkManager.Removed += new BookmarkEventHandler(BookmarkRemoved);
//			Document.LineCountChanged += BookmarkLineCountChanged;
		}
		
		/*
		void BookmarkAdded(object sender, BookmarkEventArgs e)
		{
			Bookmarks.SDBookmark b = e.Bookmark as Bookmarks.SDBookmark;
			if (b != null) {
				Bookmarks.BookmarkManager.AddMark(b);
			}
		}
		
		void BookmarkRemoved(object sender, BookmarkEventArgs e)
		{
			Bookmarks.SDBookmark b = e.Bookmark as Bookmarks.SDBookmark;
			if (b != null) {
				Bookmarks.BookmarkManager.RemoveMark(b);
			}
		}
		
		void BookmarkLineCountChanged(object sender, LineCountChangeEventArgs e)
		{
			foreach (Bookmark b in Document.BookmarkManager.Marks) {
				if (b.LineNumber >= e.LineStart) {
					Bookmarks.SDBookmark sdb = b as Bookmarks.SDBookmark;
					if (sdb != null) {
						sdb.RaiseLineNumberChanged();
					}
				}
			}
		}*/
		
		protected override void InitializeTextAreaControl(TextAreaControl newControl)
		{
			base.InitializeTextAreaControl(newControl);
			
			newControl.ShowContextMenu += delegate(object sender, MouseEventArgs e) {
				if (contextMenuPath != null) {
					MenuService.ShowContextMenu(this, contextMenuPath, (Control)sender, e.X, e.Y);
				}
			};
			newControl.TextArea.KeyEventHandler += new ICSharpCode.TextEditor.KeyEventHandler(HandleKeyPress);
			newControl.TextArea.ClipboardHandler.CopyText += new CopyTextEventHandler(ClipboardHandlerCopyText);
			
//			newControl.TextArea.IconBarMargin.Painted   += new MarginPaintEventHandler(PaintIconBarBreakPoints);
//			newControl.TextArea.IconBarMargin.MouseDown += new MarginMouseEventHandler(IconBarMouseDown);
			
			newControl.MouseWheel                       += new MouseEventHandler(TextAreaMouseWheel);
			newControl.DoHandleMousewheel = false;
		}
		
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (disposing) {
				if (errorDrawer != null) {
					errorDrawer.Dispose();
					errorDrawer = null;
				}
				if (advancedHighlighter != null) {
					advancedHighlighter.Dispose();
					advancedHighlighter = null;
				}
				CloseCodeCompletionWindow(this, EventArgs.Empty);
				CloseInsightWindow(this, EventArgs.Empty);
			}
		}
		
		void CloseCodeCompletionWindow(object sender, EventArgs e)
		{
			if (codeCompletionWindow != null) {
				codeCompletionWindow.Closed -= new EventHandler(CloseCodeCompletionWindow);
				codeCompletionWindow.Dispose();
				codeCompletionWindow = null;
			}
		}
		
		void CloseInsightWindow(object sender, EventArgs e)
		{
			if (insightWindow != null) {
				insightWindow.Closed -= new EventHandler(CloseInsightWindow);
				insightWindow.Dispose();
				insightWindow = null;
			}
		}
		
		void TextAreaMouseWheel(object sender, MouseEventArgs e)
		{
			TextAreaControl textAreaControl = (TextAreaControl)sender;
			if (insightWindow != null && !insightWindow.IsDisposed && insightWindow.Visible) {
				insightWindow.HandleMouseWheel(e);
			} else if (codeCompletionWindow != null && !codeCompletionWindow.IsDisposed && codeCompletionWindow.Visible) {
				codeCompletionWindow.HandleMouseWheel(e);
			} else {
				textAreaControl.HandleMouseWheel(e);
			}
		}
		
		void ClipboardHandlerCopyText(object sender, CopyTextEventArgs e)
		{
			TextEditorSideBar.Instance.PutInClipboardRing(e.Text);
		}
		
		public override void OptionsChanged()
		{
			base.OptionsChanged();
			SharpDevelopTextEditorProperties sdtep = base.TextEditorProperties as SharpDevelopTextEditorProperties;
			
			if (sdtep != null) {
				if (sdtep.UnderlineErrors) {
					if (errorDrawer == null) {
						errorDrawer = new ErrorDrawer(this);
					}
				} else {
					if (errorDrawer != null) {
						errorDrawer.Dispose();
						errorDrawer = null;
					}
				}
			}
		}
		
		internal void FileLoaded()
		{
			if (errorDrawer != null) {
				errorDrawer.UpdateErrors();
			}
		}
		
		#if DEBUG
		internal const Keys DebugBreakModifiers = Keys.Control | Keys.Shift | Keys.Alt;
		#endif
		
		void GenerateEditActions()
		{
			try {
				IEditAction[] actions = (IEditAction[])(AddInTree.GetTreeNode(editActionsPath).BuildChildItems(this)).ToArray(typeof(IEditAction));
				
				foreach (IEditAction action in actions) {
					foreach (Keys key in action.Keys) {
						editactions[key] = action;
					}
				}
			} catch (TreePathNotFoundException) {
				LoggingService.Warn("EditAction " + editActionsPath + " doesn't exists in the AddInTree");
			}
		}
		
		public void ActivateQuickClassBrowserOnDemand()
		{
			
		}
		
		protected override void OnFileNameChanged(EventArgs e)
		{
			base.OnFileNameChanged(e);
			ActivateQuickClassBrowserOnDemand();
		}
		
		static ICodeCompletionBinding[] codeCompletionBindings;
		
		public static ICodeCompletionBinding[] CodeCompletionBindings {
			get {
				if (codeCompletionBindings == null) {
					try {
						codeCompletionBindings = (ICodeCompletionBinding[])(AddInTree.GetTreeNode("/AddIns/DefaultTextEditor/CodeCompletion").BuildChildItems(null)).ToArray(typeof(ICodeCompletionBinding));
					} catch (TreePathNotFoundException) {
						codeCompletionBindings = new ICodeCompletionBinding[] {};
					}
				}
				return codeCompletionBindings;
			}
		}
		
		InsightWindow insightWindow = null;
		CodeCompletionWindow codeCompletionWindow = null;
		bool inHandleKeyPress;
		
		bool HandleKeyPress(char ch)
		{
			if (inHandleKeyPress)
				return false;
			inHandleKeyPress = true;
			try {
				if (codeCompletionWindow != null && !codeCompletionWindow.IsDisposed) {
					if (codeCompletionWindow.ProcessKeyEvent(ch)) {
						return true;
					}
					if (codeCompletionWindow != null && !codeCompletionWindow.IsDisposed) {
						// code-completion window is still opened but did not want to handle
						// the keypress -> don't try to restart code-completion
						return false;
					}
				}
				
				if (CodeCompletionOptions.EnableCodeCompletion) {
					foreach (ICodeCompletionBinding ccBinding in CodeCompletionBindings) {
						CodeCompletionKeyPressResult result = ccBinding.HandleKeyPress(adapter, ch);
						if (result == CodeCompletionKeyPressResult.Completed)
							return false;
						else if (result == CodeCompletionKeyPressResult.CompletedIncludeKeyInCompletion)
							return false;
						else if (result == CodeCompletionKeyPressResult.EatKey)
							return true;
					}
					if (ch == '\n')
						StartDelayedReparse();
				}
			} catch (Exception ex) {
				LogException(ex);
			} finally {
				inHandleKeyPress = false;
			}
			return false;
		}
		
		bool startedDelayedReparse;
		
		void StartDelayedReparse()
		{
			if (startedDelayedReparse)
				return;
			startedDelayedReparse = true;
			WorkbenchSingleton.SafeThreadAsyncCall(
				delegate {
					startedDelayedReparse = false;
					if (!this.IsDisposed) {
						ParserService.BeginParse(this.FileName, this.adapter.Document);
					}
				});
		}
		
		public void StartCtrlSpaceCompletion()
		{
			foreach (ICodeCompletionBinding ccBinding in CodeCompletionBindings) {
				if (ccBinding.CtrlSpace(adapter))
					return;
			}
		}
		
		internal bool ExpandTemplateOnTab()
		{
			string word = GetWordBeforeCaret();
			if (word != null) {
				/*CodeTemplateGroup templateGroup = CodeTemplateLoader.GetTemplateGroupPerFilename(FileName);
				if (templateGroup != null) {
					foreach (CodeTemplate template in templateGroup.Templates) {
						if (template.Shortcut == word) {
							if (word.Length > 0) {
								int newCaretOffset = DeleteWordBeforeCaret();
								//// set new position in text area
								ActiveTextAreaControl.TextArea.Caret.Position = Document.OffsetToPosition(newCaretOffset);
							}
							
							InsertTemplate(template);
							return true;
						}
					}
				}*/
			}
			return false;
		}
		
		public void ShowInsightWindow(IInsightDataProvider insightDataProvider)
		{
			if (insightWindow == null || insightWindow.IsDisposed) {
				insightWindow = new InsightWindow(WorkbenchSingleton.MainWin32Window, this);
				insightWindow.Closed += new EventHandler(CloseInsightWindow);
			}
			insightWindow.AddInsightDataProvider(insightDataProvider, this.FileName);
			insightWindow.ShowInsightWindow();
		}
		
		public bool InsightWindowVisible {
			get {
				return insightWindow != null;
			}
		}
		
		public void ShowCompletionWindow(ICompletionDataProvider completionDataProvider, char ch)
		{
			codeCompletionWindow = CodeCompletionWindow.ShowCompletionWindow(WorkbenchSingleton.MainWin32Window, this, this.FileName, completionDataProvider, ch);
			if (codeCompletionWindow != null) {
				codeCompletionWindow.Closed += new EventHandler(CloseCodeCompletionWindow);
			}
		}
		
		private void LogException(Exception ex)
		{
			ICSharpCode.Core.MessageService.ShowException(ex);
		}
		
		public string GetWordBeforeCaret()
		{
			int start = TextUtilities.FindPrevWordStart(Document, ActiveTextAreaControl.TextArea.Caret.Offset);
			return Document.GetText(start, ActiveTextAreaControl.TextArea.Caret.Offset - start);
		}
		
		public int DeleteWordBeforeCaret()
		{
			int start = TextUtilities.FindPrevWordStart(Document, ActiveTextAreaControl.TextArea.Caret.Offset);
			Document.Remove(start, ActiveTextAreaControl.TextArea.Caret.Offset - start);
			return start;
		}
		
		protected override void OnReloadHighlighting(object sender, EventArgs e)
		{
			base.OnReloadHighlighting(sender, e);
			InitializeAdvancedHighlighter();
		}
		
		public bool HighlightingExplicitlySet { get; set; }
		
		/// <summary>
		/// Explicitly set the highlighting to use. Will be persisted.
		/// </summary>
		public override void SetHighlighting(string name)
		{
			base.SetHighlighting(name);
			this.HighlightingExplicitlySet = true;
			InitializeAdvancedHighlighter();
		}
		
		public void InitializeAdvancedHighlighter()
		{
			if (advancedHighlighter != null) {
				advancedHighlighter.Dispose();
				advancedHighlighter = null;
			}
			string highlighterPath = advancedHighlighterPath + "/" + Document.HighlightingStrategy.Name;
			if (AddInTree.ExistsTreeNode(highlighterPath)) {
				IList<IAdvancedHighlighter> highlighter = AddInTree.BuildItems<IAdvancedHighlighter>(highlighterPath, this);
				if (highlighter != null && highlighter.Count > 0) {
					advancedHighlighter = highlighter[0];
					advancedHighlighter.Initialize(this);
					Document.HighlightingStrategy = new AdvancedHighlightingStrategy((DefaultHighlightingStrategy)Document.HighlightingStrategy, advancedHighlighter);
				}
			}
		}
		
		public void InitializeFormatter()
		{
			string formatterPath = formatingStrategyPath + "/" + Document.HighlightingStrategy.Name;
			var formatter = AddInTree.BuildItems<ICSharpCode.SharpDevelop.Editor.IFormattingStrategy>(formatterPath, this, false);
			if (formatter != null && formatter.Count > 0) {
				//Document.FormattingStrategy = formatter[0];
			}
		}
		
		public override string GetRangeDescription(int selectedItem, int itemCount)
		{
			return StringParser.Parse("${res:ICSharpCode.SharpDevelop.DefaultEditor.Gui.Editor.InsightWindow.NumberOfText}",
			                          new StringTagPair("CurrentMethodNumber", selectedItem.ToString("##")),
			                          new StringTagPair("NumberOfTotalMethods", itemCount.ToString("##")));
		}
		
//		public override IDeclarationViewWindow CreateDeclarationViewWindow()
//		{
//			return new HtmlDeclarationViewWindow();
//		}
		//
	}
}

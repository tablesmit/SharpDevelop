﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using ICSharpCode.Core;

namespace ICSharpCode.AvalonEdit.AddIn.Snippets
{
	/// <summary>
	/// SnippetManager singleton.
	/// </summary>
	public sealed class SnippetManager
	{
		public static readonly SnippetManager Instance = new SnippetManager();
		readonly object lockObj = new object();
		static readonly List<CodeSnippetGroup> defaultSnippets = new List<CodeSnippetGroup> {
			new CodeSnippetGroup {
				Extensions = ".cs",
				Snippets = {
					new CodeSnippet {
						Name = "for",
						Description = "for loop",
						Text = "for (int ${counter=i} = 0; ${counter} < ${end}; ${counter}++) {\n\t${Selection}\n}"
					},
					new CodeSnippet {
						Name = "foreach",
						Description = "foreach loop",
						Text = "foreach (${var} ${element} in ${collection}) {\n\t${Selection}\n}"
					},
					new CodeSnippet {
						Name = "if",
						Description = "if statement",
						Text = "if (${condition}) {\n\t${Selection}\n}"
					},
					new CodeSnippet {
						Name = "ifelse",
						Description = "if-else statement",
						Text = "if (${condition}) {\n\t${Selection}\n} else {\n\t\n}"
					},
					new CodeSnippet {
						Name = "while",
						Description = "while loop",
						Text = "while (${condition}) {\n\t${Selection}\n}"
					},
					new CodeSnippet {
						Name = "prop",
						Description = "Property",
						Text = "${type} ${toFieldName(name)};\n\npublic ${type=int} ${name=Property} {\n\tget { return ${toFieldName(name)}; }\n\tset { ${toFieldName(name)} = value; }\n}"
					},
					new CodeSnippet {
						Name = "propdp",
						Description = "Dependency Property",
						Text = "public static readonly DependencyProperty ${name}Property =" + Environment.NewLine
							+ "\tDependencyProperty.Register(\"${name}\", typeof(${type}), typeof(${ClassName})," + Environment.NewLine
							+ "\t                            new FrameworkPropertyMetadata());" + Environment.NewLine
							+ "" + Environment.NewLine
							+ "public ${type=int} ${name=Property} {" + Environment.NewLine
							+ "\tget { return (${type})GetValue(${name}Property); }" + Environment.NewLine
							+ "\tset { SetValue(${name}Property, value); }"
							+ Environment.NewLine + "}"
					},
					new CodeSnippet {
						Name = "ctor",
						Description = "Constructor",
						Text = "public ${ClassName}()\n{\t\n${Selection}\n}"
					},
					new CodeSnippet {
						Name = "switch",
						Description = "Switch statement",
						Text = "switch (${condition}) {\n\tcase ${firstcase=0}:\n\t\tbreak;\n\tdefault:\n\t\t${Selection}\n\t\tbreak;\n}"
					},
					new CodeSnippet {
						Name = "try",
						Description = "Try-catch statement",
						Text = "try {\n\t${Selection}\n} catch (Exception) {\n\t\n\tthrow;\n}"
					},
					new CodeSnippet {
						Name = "trycf",
						Description = "Try-catch-finally statement",
						Text = "try {\n\t${Selection}\n} catch (Exception) {\n\t\n\tthrow;\n} finally {\n\t\n}"
					},
					new CodeSnippet {
						Name = "tryf",
						Description = "Try-finally statement",
						Text = "try {\n\t${Selection}\n} finally {\n\t\n}"
					},
				}
			}
		};
		
		private SnippetManager() {}
		
		/// <summary>
		/// Loads copies of all code snippet groups.
		/// </summary>
		public List<CodeSnippetGroup> LoadGroups()
		{
			return PropertyService.Get("CodeSnippets", defaultSnippets);
		}
		
		/// <summary>
		/// Saves the set of groups.
		/// </summary>
		public void SaveGroups(IEnumerable<CodeSnippetGroup> groups)
		{
			lock (lockObj) {
				activeGroups = null;
				PropertyService.Set("CodeSnippets", groups.ToList());
			}
		}
		
		ReadOnlyCollection<CodeSnippetGroup> activeGroups;
		
		public ReadOnlyCollection<CodeSnippetGroup> ActiveGroups {
			get {
				lock (lockObj) {
					if (activeGroups == null)
						activeGroups = LoadGroups().AsReadOnly();
					return activeGroups;
				}
			}
		}
		
		public CodeSnippetGroup FindGroup(string extension)
		{
			foreach (CodeSnippetGroup g in ActiveGroups) {
				string[] extensions = g.Extensions.Split(';');
				foreach (string gext in extensions) {
					if (gext.Equals(extension, StringComparison.OrdinalIgnoreCase))
						return g;
				}
			}
			return null;
		}
		
		public CodeSnippet FindSnippet(string extension, string name)
		{
			CodeSnippetGroup g = FindGroup(extension);
			if (g != null) {
				return g.Snippets.FirstOrDefault(s => s.Name == name);
			}
			return null;
		}
	}
}

﻿F# Formatting: Command line tool
================================

To use F# Formatting tools via the command line, you can use the `fsdocs` dotnet tool.

    dotnet tool install FSharp.Formatting.CommandTool
    fsdocs [command] [options]

The build commands
----------------------------

The `fsdocs build`  command processes a directory containing a mix of Markdown documents `*.md` and F# Script files `*.fsx`
according to the rules of [Literate Programming](literate.html), and also generates API docs for projects
in the solution according to the rules of [API doc generation](apidocs.html)

    [lang=text]
    fsdocs build

### Content

The expected structure for a docs directory is

    docs/**/*.md                  -- markdown with embedded code, converted to html and optionally tex/ipynb
    docs/**/*.fsx                 -- fsx scripts converted to html and optionally tex/ipynb
    docs/**/*                     -- other content, copied over
    docs/**/_template.html        -- specifies the default HTML template for this directory and its contents
    docs/**/_template.tex         -- specifies Latex files should also be generated
    docs/**/_template.ipynb       -- specifies F# ipynb files should also be generated
    docs/**/_template.fsx         -- specifies F# fsx files should also be generated (even from markdown)
    docs/reference/_template.html -- optionally specifies the default template for reference docs

The output goes in `output/` by default.  Typically a `--parameters` argument is needed for substitutions in the template, e.g.

A Lunr search index is also generated for the API docs. See  [API doc generation](apidocs.html) for details about
how to add a search box to your `_template.html`.

You can experiment with the [template file of this project](https://github.com/fsprojects/FSharp.Formatting/blob/master/docs/_template.html). 

Template files are as follows:

- `_template.html` - absent, empty or contain `{{document}}` and `{{tooltips}}` placeholders.
- `_template.tex` - absent, empty or contain `{content}` placeholder.
- `_template.ipynb` - absent, empty or contain `{{cells}}` placeholder.
- `_template.fsx` - absent, empty or contain `{{code}}` placeholder.

The following substitutions are defined based on metadata that may be present in project files.
The first metadata value detected across project files is used, it is assumed these values will
be the same across all projects.

|  Substitution name     | Source               |
|:-----------------------|:----------------------------|
|   {{page-title}}       | First h1 heading in literate file, generated for API docs  |
|   {{project-name}}     | Name of .sln or containing directory |
|   {{root}}             | `<PackageProjectUrl>`         |
|   {{authors}}          | `<Authors>`                   |
|   {{repository-url}}   | `<RepositoryUrl>`             |
|   {{package-project-url}}  | `<PackageProjectUrl>`  |
|   {{package-license}}  | `<PackageLicenseExpression>`  |
|   {{package-tags}}     | `<PackageTags>`               |
|   {{copyright}}        | `<Copyright>`                 |
|   {{document}}         | generated html contents       |
|   {contents}           | generated latex contents       |
|   {{cells}}            | generated ipynb contents       |
|   {{tooltips}}         | generated html tooltips contents       |
|   {{source}}           | original script source           |

### Options

  * `--input` - Input directory containing `*.fsx` and `*.md` files and other content, defaults to `docs`.
  * `--projects` - The project files to process. Defaults to the packable projects in the solution in the current directory, else all packable projects.
  * `--output` -  Output directory, defaults to `output`
  * `--noApiDocs` -  Do not generate API docs
  * `--eval` - Use the default FsiEvaluator to actually evaluate code in documentation, defaults to `false`.
  * `--noLineNumbers` -  Line number option, defaults to `true`.
  * `--nonPublic` -  Generate docs for non-public members
  * `--xmlComments` -  Generate docs assuming XML comments not markdown comments in source code
  * `--parameters` -  A whitespace separated list of string pairs as extra text replacement patterns for the format template file.
  * `--clean` -  Clean the output directory before building (except directories starting with ".")
  * `--help` -  Display the specific help message for `convert`.

The watch command
----------------------------

The `fsdocs watch` command does the same as `fsdocs build` but in "watch" mode, waiting for changes. Only the files in the input
directory (e.g. `docs`) are watched.

    [lang=text]
    fsdocs watch

The same parameters are accepted.  Restarting may be necesssary on changes to project files.

The convert command
----------------------------

The `fsdocs convert` command processes a directory containing a mix of Markdown documents `*.md` and F# Script files `*.fsx`
according to the concept of [Literate Programming](literate.html) and the same rules used for `build` above.  It
doesn't generate API documentation.

    [lang=text]
    fsdocs convert --input docs/scripts --parameters "authors" "Tomas Petricek"

### Options

  * `--input` - Input directory containing `*.fsx` and `*.md` files. Required,
  * `--output` -  Output directory, defaults to `output`
  * `--template` -  Default template file for formatting. For HTML should contain `{{document}}` and `{{tooltips}}` tags.
  * `--prefix` -  Prefix for formatting, defaults to `fs`.
  * `--compilerOptions` -  Compiler options passed when evaluating snippets.
  * `--noLineNumbers` -  Line number option, defaults to `true`.
  * `--references` -  Turn all indirect links into references, defaults to `false`.
  * `--eval` - Use the default FsiEvaluator to actually evaluate code in documentation, defaults to `false`.
  * `--parameters` -  A whitespace separated list of string pairs as text replacement patterns for the format template file.
  * `--includeSource` -  Include sourcecode in documentation for substitution as `{{source}}`, defaults to `false`.
  * `--help` -  Display the specific help message for `convert`.
  * `--waitForKey` -  Wait for key before exit.

### Examples

For the example above:

1. The example commandline is executed in the working directory that contains the target files `lib1.dll` and `lib 2.dll` as well as the
corresponding meta-data files `lib1.xml` and `lib 2.xml`, which are the result of a previous build process of your project.

2. The output directory is in this example within the parent directory of your working directory.

3. The example assumes that the necessary `_template.html` file is present and contains the `{{document}}` tag
   for content substitution.
   
You can add further substitutions using the `--parameters` list. 



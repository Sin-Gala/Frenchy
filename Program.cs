using Antlr4.Runtime;
using Frenchy.FrenchyCore;
using System.IO;

string filename = "FrenchyCore/FrenchyTest/DevTest.fry";
string fileContent = File.ReadAllText(filename);

AntlrInputStream inputStream = new AntlrInputStream(fileContent.ToString());
FrenchyLexer speakLexer = new FrenchyLexer(inputStream);
CommonTokenStream commenTokenStream = new CommonTokenStream(speakLexer);
FrenchyParser speakParser = new FrenchyParser(commenTokenStream);

// Add error callback here

FrenchyParser.ProgramContext programContext = speakParser.program();
FrenchyVisitor visitor = new FrenchyVisitor();
visitor.Visit(programContext);
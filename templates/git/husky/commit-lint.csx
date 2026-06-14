using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

// Conventional Commits validator (https://www.conventionalcommits.org/).
// Husky.Net passes the commit message file path through --args (available here as Args).
var pattern = @"^(build|chore|ci|docs|feat|fix|perf|refactor|revert|style|test)(\([\w\-\.\/]+\))?(!)?: .{1,100}";

var messageFilePath = Args.FirstOrDefault();
if (messageFilePath is null || !File.Exists(messageFilePath))
{
    // Do not block commits on a tooling problem.
    Console.WriteLine("commit-lint: commit message file not provided; skipping.");
    return 0;
}

// First non-empty, non-comment line is the commit subject.
var subject = File
    .ReadAllLines(messageFilePath)
    .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
    ?? string.Empty;

// Allow conventional revert/merge style messages git itself generates.
if (subject.StartsWith("Merge ") || subject.StartsWith("Revert "))
{
    return 0;
}

if (Regex.IsMatch(subject, pattern))
{
    return 0;
}

Console.ForegroundColor = ConsoleColor.Red;
Console.WriteLine($"Invalid commit message: \"{subject}\"");
Console.ResetColor();
Console.WriteLine("Use Conventional Commits, e.g. 'feat(tickets): add SLA timer' or 'fix: correct null check'.");
Console.WriteLine("Allowed types: build, chore, ci, docs, feat, fix, perf, refactor, revert, style, test.");
Console.WriteLine("Spec: https://www.conventionalcommits.org/");
return 1;

using System;
using System.Collections.Generic;

namespace FastNLose.ViewModels;

public static class CategoryFactory
{
    public static SectionViewModel BuildWater(DateTime date)
    {
        var s = SectionViewModel.Create("WATER", 8, 6, 2, 6);
        s.Load(date);
        return s;
    }

    public static SectionViewModel BuildSteps(DateTime date)
    {
        var s = SectionViewModel.Create("STEPS", 0, 0, 1, 0);
        s.IsPicker = true;
        var items = new List<string>();
        for (int v = 5000; v <= 12000; v += 500)
            items.Add(v.ToString());
        s.Items = items;
        // Show STEPS inline (title and picker on one row)
        s.ForceStacked = false;
        s.Load(date);
        return s;
    }

    public static SectionViewModel BuildWorkout(DateTime date)
    {
        var s = SectionViewModel.Create("WORKOUT", 6, 6, 3, 4);
        s.Load(date);
        return s;
    }

    public static SectionViewModel BuildWalk(DateTime date)
    {
        var s = SectionViewModel.Create("WALK", 6, 6, 3, 6);
        // Friendly title with duration
        s.Title = "WALK-5min";
        s.Load(date);
        return s;
    }

    public static SectionViewModel BuildEvils(DateTime date)
    {
        // keep existing for backward compat
        var s = SectionViewModel.Create("EVILS", 4, 6, 5, 4, rectangle: true);
        s.Load(date);
        return s;
    }

    // Creates a simple yes/no (single-slot) section; key is sanitized for storage.
    public static SectionViewModel BuildYesNo(string question, DateTime date)
    {
        var key = SanitizeKey(question);
        var s = SectionViewModel.Create(key, 0, 0, 1, 0);
        s.IsToggle = true;
        s.Load(date);
        // Use the original question as the displayed title
        s.Title = question;
        return s;
    }

    // Simple single-slot for weight (stores e.g. presence or a numeric entry handled elsewhere).
    public static SectionViewModel BuildWeight(DateTime date)
    {
        var s = SectionViewModel.Create("WEIGHT", 0, 0, 1, 0);
        s.IsPicker = true;
        var items = new List<string>();
        for (double v = 190.0; v <= 212.0; v += 0.5)
            items.Add(v.ToString("0.0"));
        s.Items = items;
        s.Load(date);
        return s;
    }

    public static SectionViewModel BuildSugar8AM(DateTime date)
    {
        var s = SectionViewModel.Create("SUGAR_8AM", 0, 0, 1, 0);
        s.IsPicker = true;
        var items = new List<string>();
        for (int v = 110; v <= 180; v += 5)
            items.Add(v.ToString());
        s.Items = items;
        s.Load(date);
        // Friendly title
        s.Title = "8AM-Avg";
        return s;
    }

    public static SectionViewModel BuildBan(DateTime date)
    {
        var labels = new[] { "SUGR", "REFND", "RICE", "MILK", "FRIED", "OUTS" };
        var s = SectionViewModel.CreateLabeled("BAN", labels);
        // increase gap between BAN rects (double default)
        s.SlotItemSpacing = 14; // default previously ~7, doubled to ~14
        s.Load(date);
        return s;
    }

    static string SanitizeKey(string input)
        => string.IsNullOrWhiteSpace(input)
            ? "UNKNOWN"
            : input.Replace(" ", "_").Replace("?", "").ToUpperInvariant();
}
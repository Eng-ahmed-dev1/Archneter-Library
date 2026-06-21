public static class ArgumentParser
{
    public static CommandContext Parse(string[] args)
    {
        var context = new CommandContext();

        if (args.Length == 0)
            return context;

        context.Command = args[0];

        if (args.Length > 1)
            context.ProjectName = args[1];

        for (int i = 2; i < args.Length; i++)
        {
            if (args[i].StartsWith("--") && i + 1 < args.Length)
            {
                var key = args[i];
                var value = args[i + 1];

                context.Options[key] = value;
                i++;
            }
        }

        return context;
    }
}
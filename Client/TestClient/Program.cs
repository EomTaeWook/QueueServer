// See https://aka.ms/new-console-template for more information

using TestClient;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var list = new List<User>();
        for (int i = 0; i < 5; ++i)
        {
            list.Add(new User(i));
        }

        foreach (var item in list)
        {
            item.GetWaitngTicket();
        }

        while (true)
        {
            var removed = new List<User>();
            var result = Parallel.ForEach(list, (item) =>
            {
                var response = item.CheckWaitngTicket();
                if (!string.IsNullOrEmpty(response.EntryTicket))
                {
                    removed.Add(item);
                }
            });
            if (result.IsCompleted == false)
            {
                await Task.Delay(500);
            }
            foreach (var item in removed)
            {
                item.Login();
                list.Remove(item);
            }
            await Task.Delay(10000);
        }

    }
}
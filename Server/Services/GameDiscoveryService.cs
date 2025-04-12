using Server.Database;
using Server.Managers;

namespace Server.Services;

public class GameDiscoveryService : BackgroundService
{

    private readonly IServiceProvider serviceProvider;
    
    private Timer? serviceTimer = null;
    
    private const int TIMER_INTERVAL_MS = 100 * 1000;

    private const int TIMER_START_DELAY_MS = 0;
    
    public GameDiscoveryService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            GameManager gameManager = scope.ServiceProvider.GetRequiredService<GameManager>();
            
            serviceTimer = new Timer(gameManager.ScanGamesDirectoryEvent, null,
                TIMER_START_DELAY_MS, TIMER_INTERVAL_MS);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await serviceTimer.DisposeAsync();
        
        await base.StopAsync(cancellationToken);
    }

}
using Betty.Wallet.Console;
using Betty.Wallet.Core.Application;
using Betty.Wallet.Core.Domain;
using DomainWallet = Betty.Wallet.Core.Domain.Wallet;

var wallet = new DomainWallet();
var game = new SlotGame(new SystemRandomSource());
var service = new WalletService(wallet, game);

new CommandLoop(System.Console.In, System.Console.Out, service).Run();

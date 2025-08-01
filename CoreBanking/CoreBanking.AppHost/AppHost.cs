var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
	.WithImageTag("latest")
	.WithVolume("corebanking-db", "/var/lib/postgressql/data")
	.WithLifetime(ContainerLifetime.Persistent)
	.WithPgAdmin(rbuilder =>
	{
		rbuilder.WithImageTag("latest");
	});

var coreBankingDb = postgres.AddDatabase("corebanking-db", "corebanking");

var migationService = builder.AddProject<Projects.CoreBanking_MigrationService>("corebanking-migrationservice");

builder.AddProject<Projects.CoreBanking_API>("corebanking-api")
	.WithReference(coreBankingDb)
	.WaitFor(postgres)
	.WaitForCompletion(migationService);

builder.Build().Run();

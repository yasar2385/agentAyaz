using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImpactSupport.Api.Migrations
{
    public partial class AddTestingTypeClientAliasesAndSharedRoles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
ALTER TABLE MasterTemplate ADD COLUMN MasterIsSharedRole INTEGER NOT NULL DEFAULT 0;

CREATE TABLE IF NOT EXISTS MasterTestingTypeAliases (
  Alias TEXT PRIMARY KEY,
  TestingTypeId INTEGER NOT NULL REFERENCES MasterTestingTypes(Id)
);

CREATE TABLE IF NOT EXISTS ClientAliases (
  Alias TEXT PRIMARY KEY,
  ClientId INTEGER NOT NULL REFERENCES Clients(Id)
);

CREATE TABLE IF NOT EXISTS MasterTemplateClients (
  MasterId INTEGER NOT NULL REFERENCES MasterTemplate(MasterId) ON DELETE CASCADE,
  ClientId INTEGER NOT NULL REFERENCES Clients(Id),
  PRIMARY KEY (MasterId, ClientId)
);

INSERT OR IGNORE INTO Clients (Id, Code, Name) VALUES
  (1, 'OSO', 'OSO'),
  (2, 'OXMEDO', 'OxfordMed'),
  (3, 'TNF', 'T & F');

INSERT OR IGNORE INTO MasterTestingTypeAliases (Alias, TestingTypeId) VALUES
  ('Tomcat_Regression', 5);

INSERT OR IGNORE INTO ClientAliases (Alias, ClientId) VALUES
  ('T & F', 3),
  ('T&F', 3),
  ('OxfordMed', 2);
""");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
DROP TABLE IF EXISTS MasterTemplateClients;
DROP TABLE IF EXISTS ClientAliases;
DROP TABLE IF EXISTS MasterTestingTypeAliases;
""");
        }
    }
}

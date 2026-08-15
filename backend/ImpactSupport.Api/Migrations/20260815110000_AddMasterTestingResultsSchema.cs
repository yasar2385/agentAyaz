using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImpactSupport.Api.Migrations
{
    public partial class AddMasterTestingResultsSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
CREATE TABLE IF NOT EXISTS MasterModules (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL UNIQUE);
CREATE TABLE IF NOT EXISTS MasterPreconditionRoles (Id INTEGER PRIMARY KEY AUTOINCREMENT, Value TEXT NOT NULL UNIQUE);
CREATE TABLE IF NOT EXISTS MasterTestingTypes (Id INTEGER PRIMARY KEY AUTOINCREMENT, Value TEXT NOT NULL UNIQUE);
CREATE TABLE IF NOT EXISTS MasterIssueTypes (Id INTEGER PRIMARY KEY AUTOINCREMENT, Value TEXT NOT NULL UNIQUE);
CREATE TABLE IF NOT EXISTS MasterQaStatuses (Id INTEGER PRIMARY KEY AUTOINCREMENT, Value TEXT NOT NULL UNIQUE);
CREATE TABLE IF NOT EXISTS MasterDevStatuses (Id INTEGER PRIMARY KEY AUTOINCREMENT, Value TEXT NOT NULL UNIQUE);
CREATE TABLE IF NOT EXISTS Clients (Id INTEGER PRIMARY KEY AUTOINCREMENT, Code TEXT NOT NULL UNIQUE, Name TEXT NOT NULL DEFAULT '');
CREATE TABLE IF NOT EXISTS RefStyles (Id INTEGER PRIMARY KEY AUTOINCREMENT, Value TEXT NOT NULL UNIQUE);
CREATE TABLE IF NOT EXISTS RoleWorkflows (Id INTEGER PRIMARY KEY AUTOINCREMENT, Value TEXT NOT NULL UNIQUE, IsDefault INTEGER NOT NULL DEFAULT 0);
CREATE TABLE IF NOT EXISTS Types (Id INTEGER PRIMARY KEY AUTOINCREMENT, Value TEXT NOT NULL UNIQUE);
CREATE TABLE IF NOT EXISTS DtdType (Id INTEGER PRIMARY KEY AUTOINCREMENT, Value TEXT NOT NULL UNIQUE);
CREATE TABLE IF NOT EXISTS TestingUrls (Id INTEGER PRIMARY KEY AUTOINCREMENT, Value TEXT NOT NULL UNIQUE, UrlType TEXT NOT NULL);

CREATE TABLE IF NOT EXISTS MasterTemplate (
  MasterId INTEGER PRIMARY KEY AUTOINCREMENT,
  MasterTestId TEXT NOT NULL UNIQUE,
  MasterTestNo TEXT NOT NULL DEFAULT '',
  MasterSourceSheet TEXT NOT NULL DEFAULT '',
  MasterSourceRow INTEGER NOT NULL DEFAULT 0,
  MasterModules INTEGER NULL REFERENCES MasterModules(Id),
  MasterPreconditionRole INTEGER NULL REFERENCES MasterPreconditionRoles(Id),
  MasterClient INTEGER NULL REFERENCES Clients(Id),
  MasterType INTEGER NULL REFERENCES Types(Id),
  MasterDtdType INTEGER NULL REFERENCES DtdType(Id),
  MasterRoleWorkflow INTEGER NULL REFERENCES RoleWorkflows(Id),
  MasterIsCollaborative INTEGER NOT NULL DEFAULT 0,
  MasterPreparedBy TEXT NOT NULL DEFAULT '',
  MasterPreparedDate TEXT NOT NULL DEFAULT '',
  MasterTestData TEXT NOT NULL DEFAULT '',
  MasterExpectedResult TEXT NOT NULL DEFAULT '',
  MasterActualResult TEXT NOT NULL DEFAULT '',
  MasterIssueType INTEGER NULL REFERENCES MasterIssueTypes(Id),
  MasterQaStatus INTEGER NULL REFERENCES MasterQaStatuses(Id),
  MasterDevStatus INTEGER NULL REFERENCES MasterDevStatuses(Id),
  MasterCreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
  MasterUpdatedAt TEXT NOT NULL DEFAULT (datetime('now'))
);
CREATE TABLE IF NOT EXISTS MasterTestDetails (
  MasterId INTEGER PRIMARY KEY REFERENCES MasterTemplate(MasterId) ON DELETE CASCADE,
  MasterDescription TEXT NOT NULL DEFAULT '',
  MasterTestSteps TEXT NOT NULL DEFAULT ''
);
CREATE TABLE IF NOT EXISTS MasterTemplateTestingTypes (
  MasterId INTEGER NOT NULL REFERENCES MasterTemplate(MasterId) ON DELETE CASCADE,
  TestingTypeId INTEGER NOT NULL REFERENCES MasterTestingTypes(Id),
  PRIMARY KEY (MasterId, TestingTypeId)
);
CREATE TABLE IF NOT EXISTS MasterTemplateRemarks (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  MasterId INTEGER NOT NULL REFERENCES MasterTemplate(MasterId) ON DELETE CASCADE,
  RoundNumber INTEGER NOT NULL,
  QaRemark TEXT NOT NULL DEFAULT '',
  DevRemark TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS TestingMetaResults (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  Name TEXT NOT NULL,
  RunBy INTEGER NULL,
  RunAt TEXT NOT NULL DEFAULT (datetime('now')),
  RunThrough TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS TestingMetaResultLinks (
  TestingMetaResultId INTEGER NOT NULL REFERENCES TestingMetaResults(Id) ON DELETE CASCADE,
  RoleId INTEGER NOT NULL REFERENCES MasterPreconditionRoles(Id),
  TestingUrlId INTEGER NOT NULL REFERENCES TestingUrls(Id),
  PRIMARY KEY (TestingMetaResultId, RoleId)
);
CREATE TABLE IF NOT EXISTS TestingMetaResultTestingTypes (
  TestingMetaResultId INTEGER NOT NULL REFERENCES TestingMetaResults(Id) ON DELETE CASCADE,
  TestingTypeId INTEGER NOT NULL REFERENCES MasterTestingTypes(Id),
  PRIMARY KEY (TestingMetaResultId, TestingTypeId)
);
CREATE TABLE IF NOT EXISTS TestingMetaResultModuleStats (
  TestingMetaResultId INTEGER NOT NULL REFERENCES TestingMetaResults(Id) ON DELETE CASCADE,
  MasterModuleId INTEGER NOT NULL REFERENCES MasterModules(Id),
  PassCount INTEGER NOT NULL DEFAULT 0,
  FailCount INTEGER NOT NULL DEFAULT 0,
  PRIMARY KEY (TestingMetaResultId, MasterModuleId)
);
CREATE TABLE IF NOT EXISTS TestingDataResults (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  TestingMetaResultId INTEGER NOT NULL REFERENCES TestingMetaResults(Id) ON DELETE CASCADE,
  MasterTestId TEXT NOT NULL REFERENCES MasterTemplate(MasterTestId),
  MasterIssueType INTEGER NULL REFERENCES MasterIssueTypes(Id),
  MasterQaStatus INTEGER NULL REFERENCES MasterQaStatuses(Id),
  MasterDevStatus INTEGER NULL REFERENCES MasterDevStatuses(Id)
);

INSERT OR IGNORE INTO MasterTestingTypes (Id, Value) VALUES (1,'Basic'),(2,'Mock'),(3,'Browser'),(4,'Regression'),(5,'Tomcat_Reg');
INSERT OR IGNORE INTO RoleWorkflows (Id, Value, IsDefault) VALUES (1,'Author_Editor_Collator',1),(2,'Editor_Author_Collator',0),(3,'Author_Collator',0),(4,'Editor_Collator',0);
INSERT OR IGNORE INTO TestingUrls (Id, Value, UrlType) VALUES (1,'author','single'),(2,'editor','single'),(3,'collator','single'),(4,'shared_author','multi_author'),(5,'shared_editor','multi_author'),(6,'shared_collator','multi_author');
INSERT OR IGNORE INTO Types (Id, Value) VALUES (1,'Journal'),(2,'Book');
INSERT OR IGNORE INTO DtdType (Id, Value) VALUES (1,'JATS'),(2,'BITS'),(3,'DOCBOOK');
INSERT OR IGNORE INTO MasterPreconditionRoles (Id, Value) VALUES (1,'Author'),(2,'PE'),(3,'Collator'),(4,'Editor');
INSERT OR IGNORE INTO MasterIssueTypes (Id, Value) VALUES (1,'Bug'),(2,'Change Request'),(3,'Enhancement');
INSERT OR IGNORE INTO MasterQaStatuses (Id, Value) VALUES (1,'Pass'),(2,'Fail'),(3,'Fixed'),(4,'Rejected'),(5,'WIP');
INSERT OR IGNORE INTO MasterDevStatuses (Id, Value) VALUES (1,'Fixed'),(2,'Rejected'),(3,'WIP'),(4,'Open');
""");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
DROP TABLE IF EXISTS TestingDataResults;
DROP TABLE IF EXISTS TestingMetaResultModuleStats;
DROP TABLE IF EXISTS TestingMetaResultTestingTypes;
DROP TABLE IF EXISTS TestingMetaResultLinks;
DROP TABLE IF EXISTS TestingMetaResults;
DROP TABLE IF EXISTS MasterTemplateRemarks;
DROP TABLE IF EXISTS MasterTemplateTestingTypes;
DROP TABLE IF EXISTS MasterTestDetails;
DROP TABLE IF EXISTS MasterTemplate;
DROP TABLE IF EXISTS TestingUrls;
DROP TABLE IF EXISTS DtdType;
DROP TABLE IF EXISTS Types;
DROP TABLE IF EXISTS RoleWorkflows;
DROP TABLE IF EXISTS RefStyles;
DROP TABLE IF EXISTS Clients;
DROP TABLE IF EXISTS MasterDevStatuses;
DROP TABLE IF EXISTS MasterQaStatuses;
DROP TABLE IF EXISTS MasterIssueTypes;
DROP TABLE IF EXISTS MasterTestingTypes;
DROP TABLE IF EXISTS MasterPreconditionRoles;
DROP TABLE IF EXISTS MasterModules;
""");
        }
    }
}

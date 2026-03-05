package gofiles

// CouchService defines the interface for interacting with CouchDB.
type CouchService interface {
	GetDocumentByDatabaseNameAndDocumentId(dbname, id string) (map[string]interface{}, error)
	GetDocumentsByDatabaseName(dbname string) ([]map[string]interface{}, error)
	CreateDocumentByDatabaseName(dbname string, doc map[string]interface{}) error
	UpdateDocumentByDatabaseNameAndDocumentId(dbname, id string, doc map[string]interface{}) error
	DeleteDocumentByDatabaseNameAndDocumentId(dbname, id string) error
	GetDatabaseByName(dbname string) (map[string]interface{}, error)
	CreateDatabaseByName(dbname string) error
	DeleteDatabaseByName(dbname string) error
	CheckHealth() error
}

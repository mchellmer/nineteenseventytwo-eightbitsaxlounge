package gofiles

// CouchService defines the interface for interacting with CouchDB.
type CouchService interface {
	GetDoc(dbname, id string) (map[string]interface{}, error)
	CreateDoc(dbname string, doc map[string]interface{}) error
	UpdateDoc(dbname, id string, doc map[string]interface{}) error
	DeleteDoc(dbname, id string) error
	GetDb(dbname string) (map[string]interface{}, error)
	CreateDb(dbname string) error
	DeleteDb(dbname string) error
}

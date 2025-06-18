package gofiles

type CouchService interface {
	GetDoc(dbname, id string) (map[string]interface{}, error)
	CreateDoc(dbname string, doc map[string]interface{}) error
	UpdateDoc(dbname, id string, doc map[string]interface{}) error
	DeleteDoc(dbname, id string) error
	CreateDb(dbname string) error
}

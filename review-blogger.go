package main

import (
	"bytes"
	"fmt"
	"log"
	"os"
	"os/exec"
	"path/filepath"
	"regexp"
	"strings"

	"github.com/PuerkitoBio/goquery"
)

func decorateDocForBlog(doc *goquery.Document, baseUri string, idPrefix string) {
	doc.Find("a").Each(func(_ int, s *goquery.Selection) {
		href, _ := s.Attr("href")
		if !strings.HasPrefix(href, "#") {
			return
		}
		s.SetAttr("href", baseUri+"#"+idPrefix+href[1:])
	})
	doc.Find("a").Each(func(_ int, s *goquery.Selection) {
		class, _ := s.Attr("class")
		if class != "noteref" && class != "footnote" {
			return
		}
		id, _ := s.Attr("id")
		s.SetAttr("id", idPrefix+id)
	})
	doc.Find("div.footnote").Each(func(_ int, s *goquery.Selection) {
		id, _ := s.Attr("id")
		s.SetAttr("id", idPrefix+id)
	})
}

func getBlogContent(inDoc *goquery.Document) (string, string) {
	// prevent affecting original tree
	doc := inDoc.Clone()
	headline := doc.Find("h1")
	title := headline.Text()
	headline.Remove()
	body, _ := doc.Find("body").Html()
	return title, body
}

func main() {
	if len(os.Args) < 3 {
		fmt.Println("usage: review-blogger 1505_target-file.re http://example.com/")
		os.Exit(1)
	}
	fileName := os.Args[1]
	siteUri := os.Args[2]
	absPath, _ := filepath.Abs(fileName)
	r, _ := regexp.Compile("^(\\d{2})(\\d{2})_(.+)\\.re$")
	docId := r.ReplaceAllString(fileName, "20$1/$2/$3")
	baseUri := siteUri + docId + ".html"
	compiler, err := exec.LookPath("review-compile")
	if err != nil {
		log.Fatal(err)
	}
	cmd := exec.Command(compiler, "--target=html", fileName)
	cmd.Dir = filepath.Dir(absPath)
	var out bytes.Buffer
	cmd.Stdout = &out
	err = cmd.Run()
	if err != nil {
		log.Fatal(err)
	}
	rd := strings.NewReader(out.String())
	doc, _ := goquery.NewDocumentFromReader(rd)
	idPrefix := strings.Replace(docId, "/", "_", -1)
	decorateDocForBlog(doc, baseUri, idPrefix)
	title, body := getBlogContent(doc)
	fmt.Println("title: " + title)
	fmt.Println(strings.Join([]string{"<div class=\"review-post\">", body, "</div>"}, ""))
}

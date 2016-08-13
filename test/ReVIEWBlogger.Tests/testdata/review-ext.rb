module ReVIEW
  class HTMLBuilder
    def id2blogger_url(id)
      id.gsub(/^(\d{2})(\d{2})_/, '/20\1/\2/')
    end
    def inline_chapref(id)
      title = super
      relative_url = id2blogger_url(id)
      %Q(<a href="#{relative_url}#{extname}">#{title}</a>)
    end
  end
  class Builder
    def headline_prefix(level)
	  ['', nil]
    end
  end
end

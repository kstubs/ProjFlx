<?xml version="1.0" encoding="utf-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:wbt="myWebTemplater.1.0" xmlns:sbt="mySiteTemplater.1.0" xmlns:pbt="myPageTemplater.1.0"
	exclude-result-prefixes="wbt sbt pbt">

	<!-- includes -->
	<xsl:include href="WBT_FORMS.xsl"/>
	<xsl:include href="WBT_TABLES.xsl"/>
	<xsl:include href="WBT_LINKS.xsl"/>
	<xsl:include href="WBT_PAGING.xsl"/>
	<xsl:include href="BS_TWITTER_3.1.xsl"/>

	<xsl:output method="html" indent="yes" encoding="utf-8"/>

	<xsl:param name="wrap-content" select="false()"/>
	<xsl:param name="projSql" select="/.."/>
	
	<!-- INTERNAL PRIVATE PARAMTER VARIABLES -->
	<xsl:param name="DOC_ACTION"/>
	<xsl:param name="DOC_FOLDER"/>
	<xsl:param name="OUT">HTML</xsl:param>

	<xsl:param name="LoggedOnUser" select="false()"/>
	<xsl:param name="AuthenticatedUser" select="false()"/>
	<xsl:param name="DEBUG" select="false()"/>

	<xsl:variable name="HTTP_METHOD" select="/flx/proj/browser/page/HTTP_METHOD/item[1]"/>
	<xsl:variable name="BrowserPage" select="/flx/proj/browser/page/PAGE_HEIRARCHY/item"/>
	<xsl:variable name="PageHeirarchyCombined">
		<xsl:for-each select="$BrowserPage">
			<xsl:value-of select="."/>
			<xsl:if test="not(position() = last())">
				<xsl:text>.</xsl:text>
			</xsl:if>
		</xsl:for-each>
	</xsl:variable>
	
	<!-- Browser Vars -->
	<xsl:variable name="wbt:browser-vars" select="/flx/proj/browser/formvars | /flx/proj/browser/queryvars | /flx/proj/browser/cookievars"/> 
	
	<!-- Results Variable -->
	<xsl:variable name="wbt:results" select="ROOT/APPLICATION/results"/>

	<!-- KEYS for Browser Vars -->
	<xsl:key name="wbt:key_FormVars" match="formvars/element" use="@name"/>
	<xsl:key name="wbt:key_QueryVars" match="queryvars/element" use="@name"/>
	<xsl:key name="wbt:key_BrowserVars" match="formvars/element | queryvars/element" use="@name"/>
	<!-- synonymous with wbt:key_FormAndQueryVars -->
	<xsl:key name="wbt:key_FormAndQueryVars" match="formvars/element | queryvars/element" use="@name"/>
	<xsl:key name="wbt:key_CookieVars" match="cookievars/element" use="@name"/>
	<xsl:key name="wbt:key_ServerVars" match="servervars/element" use="@name"/>
	<xsl:key name="wbt:key_AllVars" match="browser/*/element" use="@name"/>
	<xsl:key name="wbt:capable" match="capable/element" use="@name"/>

	<!-- KEYS for Results -->
	<xsl:key name="wbt:key_QueryResults" match="wbt:app/projectResults/results" use="@name"/>
	<xsl:key name="wbt:key_Results" match="results" use="@name"/>
	<xsl:key name="wbt:key_Paging" match="paging" use="parent::results/@name"/>
	<xsl:key name="wbt:key_Schema" match="schema" use="query/@name"/>
	<xsl:key name="wbt:key_SchemaParameters" match="parameters" use="ancestor::query/@name"/>

	<!-- KEYS for edits -->
	<xsl:key name="wbt:param-name-lookupval" match="parameter[@lookup_value]" use="@name"/>

	<!-- KEYS for Tags -->
	<xsl:key name="wbt:key_Tags" match="tag[not(@Comment = 'True')]" use="@Name"/>

	<xsl:variable name="ABC" select="'ABCDEFGHIJKLMNOPQRSTUVWXYZ'"/>
	<xsl:variable name="abc" select="'abcdefghijklmnopqrstuvwxyz'"/>

	<!-- MAIN TEMPLATE MATCH -->
	<xsl:template match="/">
		<xsl:text disable-output-escaping="yes">&lt;!DOCTYPE html&gt;</xsl:text>
		<html>
			<head>
				<xsl:call-template name="wbt:metatags"/>
				<xsl:call-template name="wbt:bootstrap"/>
				<xsl:call-template name="wbt:style"/>
				<xsl:apply-templates select="." mode="wbt:javascript"/>
				<xsl:call-template name="google-universal-tracking"/>
			</head>
			<body>
				<xsl:call-template name="wbt:body-class"/>
				<xsl:call-template name="wbt:errors"/>
				<xsl:apply-templates select="." mode="wbt:header"/>
				<xsl:call-template name="wbt:body"/>
				<xsl:apply-templates select="." mode="wbt:footer"/>
				<xsl:call-template name="Google-Analytics"/>
			</body>
		</html>
	</xsl:template>

	<xsl:template name="wbt:body-class"/>
	<xsl:template name="google-universal-tracking"/>
	<xsl:template name="Google-Analytics"/>

	<xsl:variable name="wbt:error_tags" select="key('wbt:key_Tags', 'ProjectFLX_ERROR') | key('wbt:key_Tags', 'UNHANDLED_ERROR') | key('wbt:key_Tags', 'STACK_TRACE')"/>

	<!-- Unhandled Exceptions caught here -->
	<xsl:template name="wbt:errors">
		<xsl:call-template name="wbt:comment">
			<xsl:with-param name="comment">wbt:errors</xsl:with-param>
		</xsl:call-template>
		<xsl:if test="$wbt:error_tags and $DEBUG">
			<div class="container">
				<div class="unhandled-exception-container">
					<table id="wbt-errors" class="table table-bordered" style="display:none">
						<tbody>
							<xsl:for-each select="$wbt:error_tags">
								<tr>
									<xsl:choose>
										<xsl:when test="@Name[. = 'ProjectFLX_ERROR' or . = 'UNHANDLED_ERROR']">
											<th>
												<xsl:value-of select="@Name"/>
											</th>
											<td>
												<xsl:value-of select="@Class"/>
											</td>
											<td>
												<xsl:value-of select="@Method"/>
											</td>
											<td>
												<xsl:value-of select="."/>
											</td>
										</xsl:when>
										<xsl:when test="@Name = 'STACK_TRACE'">
											<th>
												<xsl:value-of select="@Name"/>
											</th>
											<td colspan="3">
												<xsl:value-of select="."/>
											</td>
										</xsl:when>
									</xsl:choose>
								</tr>
							</xsl:for-each>
						</tbody>
					</table>
				</div>
			</div>
			<script>
        function showErrors(e) {
          e.stop();
          $('wbt-errors').show();          
        }
        document.on('click', 'div.unhandled-exception-container', showErrors.bindAsEventListener());
      </script>
		</xsl:if>
	</xsl:template>

	<xsl:template name="wbt:bootstrap">
		<xsl:call-template name="wbt:comment">
			<xsl:with-param name="comment">wbt:bootstrap</xsl:with-param>
		</xsl:call-template>
		<link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/bootstrap/3.3.1/css/bootstrap.min.css" type="text/css"/>
		<xsl:text>&#10;</xsl:text>
		<link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/bootstrap/3.3.1/css/bootstrap-theme.min.css" type="text/css"/>
		<xsl:text>&#10;</xsl:text>
		<link rel="stylesheet" href="//maxcdn.bootstrapcdn.com/font-awesome/4.4.0/css/font-awesome.min.css" type="text/css"/>
		<xsl:text>&#10;</xsl:text>
		<script src="//cdn.xportability.com/js/protoscripty__130455207001161849.js" type="text/javascript"/>
		<xsl:text>&#10;</xsl:text>
		<script src="http://www2.meetscoresonline.com/ProjectFLX/bootstrap-prototype/3.3.1/transition_prototype.js" type="text/javascript"></script>
		<xsl:text>&#10;</xsl:text>
		<script src="http://www2.meetscoresonline.com/ProjectFLX/bootstrap-prototype/3.3.1/affix_prototype.js" type="text/javascript"></script>
		<xsl:text>&#10;</xsl:text>
		<script src="http://www2.meetscoresonline.com/ProjectFLX/bootstrap-prototype/3.3.1/alert_prototype.js" type="text/javascript"></script>
		<xsl:text>&#10;</xsl:text>
		<script src="http://www2.meetscoresonline.com/ProjectFLX/bootstrap-prototype/3.3.1/carousel_prototype.js" type="text/javascript"></script>
		<xsl:text>&#10;</xsl:text>
		<script src="http://www2.meetscoresonline.com/ProjectFLX/bootstrap-prototype/3.3.1/button_prototype.js" type="text/javascript"></script>
		<xsl:text>&#10;</xsl:text>
		<script src="http://www2.meetscoresonline.com/ProjectFLX/bootstrap-prototype/3.3.1/collapse_prototype.js" type="text/javascript"></script>
		<xsl:text>&#10;</xsl:text>
		<script src="http://www2.meetscoresonline.com/ProjectFLX/bootstrap-prototype/3.3.1/scrollspy_prototype.js" type="text/javascript"></script>
		<xsl:text>&#10;</xsl:text>
		<script src="http://www2.meetscoresonline.com/ProjectFLX/bootstrap-prototype/3.3.1/dropdown_prototype.js" type="text/javascript"></script>
		<xsl:text>&#10;</xsl:text>
	</xsl:template>

	<xsl:template name="wbt:metatags">
		<xsl:call-template name="wbt:comment">
			<xsl:with-param name="comment">wbt:metatags</xsl:with-param>
		</xsl:call-template>
		<xsl:call-template name="wbt:metatags.title"/>
		<xsl:call-template name="wbt:metatags.description"/>
		<xsl:call-template name="wbt:metatags.keywords"/>
		<xsl:call-template name="wbt:metatags.custom"/>
		<link rel="shortcut icon" href="/favicon.png"/>
	</xsl:template>

	<!--  Your custom meta tags here -->
	<xsl:template name="wbt:metatags.custom">
		<xsl:call-template name="wbt:comment">
			<xsl:with-param name="comment">wbt:metatags.custom</xsl:with-param>
		</xsl:call-template>
		<xsl:call-template name="sbt:metatags.custom"/>
	</xsl:template>

	<!--  Your Site Level custom meta tags here -->
	<xsl:template name="sbt:metatags.custom">
		<xsl:call-template name="pbt:metatags.custom"/>
	</xsl:template>

	<!--  Your Page Level custom meta tags here -->
	<xsl:template name="pbt:metatags.custom"/>

	<!-- Default Page Title -->
	<xsl:template name="wbt:metatags.title">
		<xsl:param name="value">ProjectFlx - Template</xsl:param>
		<xsl:call-template name="wbt:comment">
			<xsl:with-param name="comment">wbt:metatags.title</xsl:with-param>
		</xsl:call-template>
		<xsl:choose>
			<xsl:when test="/flx/proj/browser/page/TITLE/item[@name = $PageHeirarchyCombined]">
				<title>
					<xsl:value-of select="/flx/proj/browser/page/TITLE/item[@name = $PageHeirarchyCombined]"/>
				</title>
			</xsl:when>
			<xsl:when test="/flx/proj/browser/page/TITLE">
				<title>
					<xsl:value-of select="/flx/proj/browser/page/TITLE/item[last()]"/>
				</title>
			</xsl:when>
			<xsl:otherwise>
				<title>
					<xsl:value-of select="$value"/>
				</title>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<!-- Default description meta tags -->
	<xsl:template name="wbt:metatags.description">
		<xsl:param name="value">ProjectFlx - Template</xsl:param>
		<xsl:call-template name="wbt:comment">
			<xsl:with-param name="comment">wbt:metatags.description</xsl:with-param>
		</xsl:call-template>
		<xsl:choose>
			<xsl:when test="/flx/proj/browser/page/DESCRIPTION/item[@name = $PageHeirarchyCombined]">
				<meta name="description" value="{/flx/proj/browser/page/DESCRIPTION/item[@name=$PageHeirarchyCombined]}"/>
			</xsl:when>
			<xsl:when test="/flx/proj/browser/page/DESCRIPTION">
				<meta name="description" value="{/flx/proj/browser/page/DESCRIPTION/item[last()]}"/>
			</xsl:when>
			<xsl:otherwise>
				<meta name="keywords" value="{$value}"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<!-- Default keyword meta tags -->
	<xsl:template name="wbt:metatags.keywords">
		<xsl:param name="value">ProjectFlx - Template</xsl:param>
		<xsl:call-template name="wbt:comment">
			<xsl:with-param name="comment">wbt:metatags.keywords</xsl:with-param>
		</xsl:call-template>
		<xsl:choose>
			<xsl:when test="/flx/proj/browser/page/KEYWORDS/item[@name = $PageHeirarchyCombined]">
				<meta name="keywords" value="{/flx/proj/browser/page/KEYWORDS/item[@name=$PageHeirarchyCombined]}"/>
			</xsl:when>
			<xsl:when test="/flx/proj/browser/page/KEYWORDS">
				<meta name="keywords" value="{/flx/proj/browser/page/KEYWORDS/item[last()]}"/>
			</xsl:when>
			<xsl:otherwise>
				<meta name="keywords" value="{$value}"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template match="/" mode="wbt:javascript">
		<xsl:call-template name="wbt:comment">
			<xsl:with-param name="comment">wbt:javascript</xsl:with-param>
		</xsl:call-template>
		<script src="http://www2.meetscoresonline.com/ProjectFLX/Documents/event.simulate.js" type="text/javascript"/>
		<xsl:text>&#10;</xsl:text>
		<script src="http://www2.meetscoresonline.com/ProjectFLX/Documents/WBT_SCRIPT.js" type="text/javascript"/>
		<xsl:text>&#10;</xsl:text>

		<!--Embeded javascript-->
		<xsl:call-template name="wbt:comment">
			<xsl:with-param name="comment">Embeded javascript</xsl:with-param>
		</xsl:call-template>
		<xsl:for-each select="/flx/proj/browser/page/SCRIPT/item">
			<xsl:text>&#10;</xsl:text>
			<script src="{.}"/>
		</xsl:for-each>
		<xsl:call-template name="wbt:comment">
			<xsl:with-param name="comment">Raw javascript</xsl:with-param>
		</xsl:call-template>
		<xsl:for-each select="/flx/proj/browser/page/RAW_SCRIPT/item">
			<xsl:text>&#10;</xsl:text>
			<script><xsl:value-of select="." disable-output-escaping="yes"/></script>
		</xsl:for-each>

		<!--Site level javascript-->
		<xsl:call-template name="wbt:comment">
			<xsl:with-param name="comment">Site level javascript</xsl:with-param>
		</xsl:call-template>
		<xsl:call-template name="sbt:javascript"/>
		<xsl:text>&#10;</xsl:text>

		<!--Page level javascript-->
		<xsl:call-template name="wbt:comment">
			<xsl:with-param name="comment">Page level javascript</xsl:with-param>
		</xsl:call-template>
		<xsl:call-template name="pbt:javascript"/>

		<!--Backend pass-through script-->
		<xsl:call-template name="wbt:comment">
			<xsl:with-param name="comment">Backend pass-through script</xsl:with-param>
		</xsl:call-template>
		<script type="text/javascript">
            <xsl:text>&#10;wbt.queryvars={</xsl:text>
            <xsl:for-each select="/flx/proj/browser/queryvars/element">
                <xsl:value-of select="concat('&quot;', @name, '&quot;: &quot;', ., '&quot;')"/>
                <xsl:if test="not(position() = last())">
                    <xsl:text>, </xsl:text>
                </xsl:if>
            </xsl:for-each>
            <xsl:text>}&#10;</xsl:text>
            <!--
				pass through VARS//-->
            <xsl:for-each select="/flx/proj/browser/page/VARS/item">
                <xsl:text>&#10;wbt.</xsl:text>
                <xsl:value-of select="@name"/>
                <xsl:text>=</xsl:text>
                <xsl:value-of select="."/>
                <xsl:text>;</xsl:text>
            </xsl:for-each>
        </script>

	</xsl:template>

	<xsl:template name="sbt:javascript"/>
	<xsl:template name="pbt:javascript"/>


	<xsl:template name="wbt:style">
		<xsl:call-template name="wbt:comment">
			<xsl:with-param name="comment">wbt:style</xsl:with-param>
		</xsl:call-template>
		<!-- TODO: map this to CDN repo -->
		<link rel="stylesheet" href="http://www2.meetscoresonline.com/ProjectFLX/Documents/WBT_STYLE.css" type="text/css"/>
		<xsl:text>&#10;</xsl:text>
		<link rel="stylesheet" href="http://www2.meetscoresonline.com/ProjectFLX/Documents/social-buttons.css" type="text/css"/>
		<xsl:text>&#10;</xsl:text>
		<xsl:call-template name="sbt:style"/>
		<xsl:call-template name="pbt:style"/>
		<xsl:for-each select="/flx/proj/browser/page/STYLE/item">
			<xsl:text>&#10;</xsl:text>
			<link rel="stylesheet" href="{.}" type="text/css"/>
		</xsl:for-each>
		<xsl:call-template name="sbt:media-style"/>
		<xsl:apply-templates select="/flx/client//pbt:style"/>
	</xsl:template>

	<xsl:template name="sbt:media-style"/>
	<xsl:template name="sbt:style"/>
	<xsl:template name="pbt:style">
		<link href="http://getbootstrap.com/assets/css/docs.min.css" rel="stylesheet" type="text/css"/>
	</xsl:template>

	<!-- This is a main template.  Override this to setup a header for your website -->
	<xsl:template match="/" mode="wbt:header">
		<xsl:call-template name="wbt:comment">
			<xsl:with-param name="comment">wbt:header</xsl:with-param>
		</xsl:call-template>
		<header>
			<div class="container">ProjectFlx - Header Region</div>
		</header>
	</xsl:template>

	<!-- This is a main template.  Override this to setup body for your website.
        NOTE:  you must invoke wbt:walk-content from this template to properly crawl your ProjectFLX xml content -->
	<xsl:template match="/" name="wbt:body" mode="wbt:body">
		<xsl:call-template name="wbt:comment">
			<xsl:with-param name="comment">wbt:body</xsl:with-param>
		</xsl:call-template>
		<div class="container">
			<div class="row">
				<div class="col-md-8">
					<h1>ProjectFlx</h1>
				</div>
				<div class="col-md-1">
					<a href="/projectflx.selfdoc" title="Self Documenting Template">Self Doc</a>
				</div>
			</div>
			<div class="row">
				<div class="col-md-12">
					<xsl:call-template name="wbt:breadcrumb"/>
				</div>
			</div>
			<div class="row">
				<div class="col-md-3">
					<ul class="nav nav-list affix">
						<xsl:apply-templates select="/flx/client-context/page/content[not(@active='false')]" mode="identity-build-nav"/>
					</ul>
				</div>
				<div class="col-md-9">
					<xsl:call-template name="wbt:walk-content"/>
				</div>
			</div>
		</div>
	</xsl:template>

	<!-- This is a main template.  Override this to setup a footer for your website -->
	<xsl:template name="wbt:footer">
		<xsl:apply-templates select="/" mode="wbt:footer"/>
	</xsl:template>

	<xsl:template match="/" mode="wbt:footer">
		<xsl:call-template name="wbt:comment">
			<xsl:with-param name="comment">wbt:footer</xsl:with-param>
		</xsl:call-template>
		<div class="footer">
			<div class="container">
				<blockquote class="pull-right">
					<p>Now get to building that website!</p>
					<small>ProjectFlx</small>
				</blockquote>
			</div>
		</div>
	</xsl:template>

	<xsl:template name="wbt:main"> </xsl:template>

	<xsl:template match="xsl:*[substring-before(@name,':') = 'wbt']" mode="wbt:transform">
		<a href="#" class="list-group-item">
			<h4>
				<i class="fa fa-th" title="Named Template: {current()/@name}"/>
				<xsl:text> </xsl:text>
				<xsl:value-of select="@name"/>
			</h4>
			<xsl:if test="preceding-sibling::comment()[generate-id(following-sibling::*[1]) = generate-id(current())]">
				<hr/>
				<p class="list-group-item-text well sm pre">
					<xsl:value-of select="preceding-sibling::comment()[1]"/>
				</p>
			</xsl:if>
		</a>
	</xsl:template>
	<xsl:template match="xsl:*[contains(@match,':wbt')]" mode="wbt:transform">
		<a href="#" class="list-group-item">
			<h4>
				<i class="fa fa-th-large" title="Matched Template: {@match}"/>
				<xsl:text> </xsl:text>
				<xsl:value-of select="@match"/>
				<xsl:if test="@mode">
					<small class="pull-right">
						<xsl:value-of select="@mode"/>
					</small>
				</xsl:if>
			</h4>
			<xsl:if test="preceding-sibling::comment()[generate-id(following-sibling::*[1]) = generate-id(current())]">
				<hr/>
				<pre><xsl:value-of select="preceding-sibling::comment()[1]"/></pre>
			</xsl:if>
		</a>
	</xsl:template>

	<!-- a trap for things not matching above -->
	<xsl:template match="*" mode="wbt:transform"/>

	<!-- USEFUL STUFF -->
	<xsl:template name="wbt:build_url">
		<xsl:param name="url"/>
		<xsl:param name="display">Visit Site</xsl:param>
		<xsl:param name="class"/>
		<xsl:param name="icon-class"/>
		<a href="{$url}">
			<xsl:if test="$class">
				<xsl:attribute name="class">
					<xsl:value-of select="$class"/>
				</xsl:attribute>
			</xsl:if>
			<xsl:if test="string($icon-class)">
				<span class="{$icon-class}"/>
				<xsl:text> </xsl:text>
			</xsl:if>
			<xsl:value-of select="$display"/>
		</a>
	</xsl:template>

	<xsl:template name="wbt:makeIMGUrl">
		<xsl:param name="src"/>
		<xsl:param name="width"/>
		<xsl:param name="height"/>
		<xsl:param name="url"/>
		<xsl:param name="Query"/>
		<xsl:param name="onclick"/>
		<xsl:param name="onmouseover"/>
		<xsl:param name="class"/>
		<xsl:variable name="q">
			<xsl:if test="string($Query)">
				<xsl:value-of select="concat('?',$Query)"/>
			</xsl:if>
		</xsl:variable>
		<a href="{$url}{$q}">
			<xsl:if test="string($onclick)">
				<xsl:attribute name="onclick">
					<xsl:value-of select="$onclick"/>
				</xsl:attribute>
			</xsl:if>
			<xsl:if test="string($onmouseover)">
				<xsl:attribute name="onmouseover">
					<xsl:value-of select="$onmouseover"/>
				</xsl:attribute>
			</xsl:if>
			<xsl:if test="$class">
				<xsl:attribute name="class">
					<xsl:value-of select="$class"/>
				</xsl:attribute>
			</xsl:if>
			<img src="{$src}" width="{$width}" height="{$height}" border="0"> </img>
		</a>
	</xsl:template>

	<xsl:template name="wbt:makeUrlClick">
		<xsl:param name="Display"/>
		<xsl:param name="url"/>
		<xsl:param name="Query"/>
		<xsl:param name="onclick"/>
		<xsl:param name="onmouseover"/>
		<xsl:param name="class"/>
		<xsl:variable name="q">
			<xsl:if test="string($Query)">
				<xsl:value-of select="concat('?',normalize-space($Query))"/>
			</xsl:if>
		</xsl:variable>
		<a href="{$url}{$q}">
			<xsl:if test="string($onclick)">
				<xsl:attribute name="onclick">
					<xsl:value-of select="normalize-space($onclick)"/>
				</xsl:attribute>
			</xsl:if>
			<xsl:if test="string($onmouseover)">
				<xsl:attribute name="onmouseover">
					<xsl:value-of select="$onmouseover"/>
				</xsl:attribute>
			</xsl:if>
			<xsl:if test="$class">
				<xsl:attribute name="class">
					<xsl:value-of select="$class"/>
				</xsl:attribute>
			</xsl:if>
			<xsl:value-of select="$Display"/>
		</a>
	</xsl:template>

	<!-- convert browsersvars to querystring -->
	<xsl:template name="wbt:AllBrowserVars_ToQueryString">
		<xsl:apply-templates select="/ROOT/TMPLT/BROWSER/node()[name() = 'FORMVARS' or name() = 'QUERYVARS']/ELEMENT" mode="wbt:QueryString"/>
	</xsl:template>

	<xsl:template match="ELEMENT" mode="wbt:QueryString">
		<xsl:value-of select="concat(@name, '=', .)"/>
		<xsl:value-of select="name()"/>
		<xsl:if test="not(position() = last())">
			<xsl:text>&amp;</xsl:text>
		</xsl:if>
	</xsl:template>

	<!-- fix web links, append http:// if missing -->
	<xsl:template name="wbt:fix_link">
		<xsl:param name="link"/>

		<xsl:choose>
			<xsl:when test="substring(translate($link, 'HTTP', 'http'), 1, 7) = 'http://'">
				<xsl:value-of select="$link"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="concat('http://', $link)"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template name="wbt:escape_val">
		<xsl:param name="val"/>
		<xsl:param name="type"/>


		<xsl:choose>
			<xsl:when test="$type = 'char'">
				<xsl:call-template name="recurse_char">
					<xsl:with-param name="char" select="$val"/>
				</xsl:call-template>
			</xsl:when>
			<xsl:when test="$type = 'int'">
				<xsl:value-of select="val"/>
			</xsl:when>
			<xsl:when test="$type = 'date'">
				<xsl:variable name="new_dt">
					<xsl:call-template name="recurse_date">
						<xsl:with-param name="dt" select="$val"/>
					</xsl:call-template>
				</xsl:variable>

				<xsl:value-of select='concat("&apos;", $new_dt, "&apos;")'/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select='concat("&apos;", $val, "&apos;")'/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template name="recurse_date">
		<xsl:param name="dt"/>

		<xsl:variable name="month" select="substring-before($dt, '/')"/>
		<xsl:variable name="day_part1" select="substring-after($dt, '/')"/>
		<xsl:variable name="day" select="substring-before($day_part1, '/')"/>
		<xsl:variable name="year" select="substring-after($day_part1, '/')"/>

		<xsl:value-of select="concat($year, '-', $month, '-', $day)"/>
	</xsl:template>

	<xsl:template name="recurse_char">
		<xsl:param name="char"/>
		<xsl:param name="safe_val"/>

		<xsl:choose>
			<xsl:when test="not(string($char))">
				<xsl:value-of select="$safe_val"/>
			</xsl:when>
			<xsl:otherwise>

				<xsl:variable name="new_val">
					<xsl:choose>
						<xsl:when test='string(substring-before($char, "&apos;"))'>
							<!-- escape single quote with double quote -->
							<xsl:value-of select='concat(substring-before($char, "&apos;"), "\&apos;")'/>
						</xsl:when>
						<xsl:otherwise>
							<xsl:value-of select="$char"/>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:variable>

				<xsl:call-template name="recurse_char">
					<xsl:with-param name="char" select='substring-after($char, "&apos;")'/>
					<!-- return characters after ' -->
					<xsl:with-param name="safe_val" select="concat($safe_val, $new_val)"/>
				</xsl:call-template>
			</xsl:otherwise>
		</xsl:choose>

	</xsl:template>

	<xsl:template match="@* | node()" mode="identity-translate">
		<xsl:param name="ignore-name-space" select="false()"/>
		<xsl:if test="$ignore-name-space or not(string(namespace-uri()))">
			<xsl:copy>
				<xsl:apply-templates select="@* | node()" mode="identity-translate"/>
			</xsl:copy>
		</xsl:if>
	</xsl:template>

	<xsl:template match="sbt:* | wbt:* | pbt:*" mode="identity-translate"/>

	<xsl:template match="sbt:* | wbt:* | pbt:*">
		<xsl:element name="div">
			<xsl:attribute name="class">
				<xsl:value-of select="concat(substring-before(name(), ':'), ' ', local-name())"/>
			</xsl:attribute>
			<xsl:apply-templates select="@* | node()" mode="identity-translate"/>
		</xsl:element>
	</xsl:template>

	<xsl:template match="self_doc" mode="identity-translate">
		<h1>Keys</h1>
		<div class="list-group">
			<xsl:apply-templates select="document('')/xsl:stylesheet/xsl:key" mode="wbt:transform">
				<xsl:sort order="descending" data-type="number" select="@priority"/>
			</xsl:apply-templates>
		</div>
		<h1>Parameters</h1>
		<div class="list-group">
			<xsl:apply-templates select="document('')/xsl:stylesheet/xsl:parameter" mode="wbt:transform">
				<xsl:sort order="descending" data-type="number" select="@priority"/>
			</xsl:apply-templates>
		</div>
		<h1>Variables</h1>
		<div class="list-group">
			<xsl:apply-templates select="document('')/xsl:stylesheet/xsl:variable" mode="wbt:transform">
				<xsl:sort order="descending" data-type="number" select="@priority"/>
			</xsl:apply-templates>
		</div>
		<h1>Templates</h1>
		<div class="list-group">
			<xsl:apply-templates select="document('')/xsl:stylesheet/xsl:template" mode="wbt:transform">
				<xsl:sort order="descending" data-type="number" select="@priority"/>
			</xsl:apply-templates>
		</div>
	</xsl:template>

	<!-- obsolete? -->
	<xsl:template name="wbt:breadcrumb">
		<ul class="breadcrumb">
			<xsl:call-template name="wbt:walk-content">
				<xsl:with-param name="type">breadcrumb</xsl:with-param>
			</xsl:call-template>
		</ul>
	</xsl:template>
	
	<xsl:template match="Crumbs" mode="identity-translate">
		<ol class="breadcrumb">
			<xsl:for-each select="$BrowserPage">
				<li>
					<xsl:choose>
						<xsl:when test="position() = last()">
							<xsl:attribute name="class">active</xsl:attribute>
							<xsl:value-of select="@title"/>
						</xsl:when>
						<xsl:otherwise>
							<a href="{@link}">
								<xsl:value-of select="@title"/>
							</a>
						</xsl:otherwise>
					</xsl:choose>
				</li>
			</xsl:for-each>
		</ol>
	</xsl:template>
	
	

	<!-- 
        *   Walk content and browser items
        *   Return either 
        *       1. Navigation for the content
        *       2. Cookie crumbs
        *       3. Page Content
        *   Look for matching page or content according to page heirarchy
        *   stars with last browser page item and return exact match of
        *   browser page name to combined page heiarchy name
        *   as a last resort, return page name equals last page heirarchy name
        *   this is because the client xml may contain exactly the 
        *   Walking Scenarios
        *       1. single page heirarchy = page name     /abc        <page name="abc"
        *       2. multi page heirarchy = page name      /abc/xyz    <page name="xyz"
        *       3. (typical) multi page heirarchy matches content  
        *                                               /abc.xyz    <page name="abc">                                                                        
        *                                                               <content name="xyz">
        *       4. multi page heirarch matches deeper level like
        *                                               /abc.xyz.123
        *                                                            <page name="abc"
        *                                                                <content name="xyz">
        *                                                                     <content name="123">
    -->
	<xsl:template name="wbt:walk-content">
		<xsl:param name="type">content</xsl:param>
		<xsl:apply-templates select="$BrowserPage[1]" mode="wbt:walk-content">
			<xsl:with-param name="type" select="$type"/>
		</xsl:apply-templates>
	</xsl:template>

	<xsl:template match="item" mode="wbt:walk-content">
		<xsl:param name="type">content</xsl:param>
		<xsl:param name="path" select="text()"/>
		<xsl:param name="context" select="/flx/client/*[local-name() = 'page' or local-name() = 'content'][translate(@name, $ABC, $abc) = current()/text()][not(@active='false')]"/>
		<xsl:param name="title">
			<xsl:choose>
				<xsl:when test="$context/@title">
					<xsl:value-of select="$context/@title"/>
				</xsl:when>
				<xsl:when
					test="$context/*[not(namespace-uri() = 'myWebTemplater.1.0' or namespace-uri() = 'mySiteTemplater.1.0' or namespace-uri() = 'myPageTemplater.1.0')][1][name() = 'h1' or name() = 'h2' or name() = 'h3']">
					<xsl:value-of
						select="$context/*[not(namespace-uri() = 'myWebTemplater.1.0' or namespace-uri() = 'mySiteTemplater.1.0' or namespace-uri() = 'myPageTemplater.1.0')][1][name() = 'h1' or name() = 'h2' or name() = 'h3']/text()"
					/>
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="text()"/>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:param>
		<xsl:if test="$type = 'breadcrumb'">
			<li>
				<xsl:choose>
					<xsl:when test="following-sibling::item">
						<a href="{$path}">
							<xsl:value-of select="$title"/>
						</a>
					</xsl:when>
					<xsl:otherwise>
						<xsl:attribute name="class">active</xsl:attribute>
						<xsl:value-of select="$title"/>
					</xsl:otherwise>
				</xsl:choose>
			</li>
		</xsl:if>
		<xsl:choose>
			<!-- scenario 1 -->
			<xsl:when test="not($context) and following-sibling::item[1]">
				<xsl:apply-templates select="following-sibling::item[1]" mode="wbt:walk-content">
					<xsl:with-param name="path" select="concat($path, '/', following-sibling::item[1]/text())"/>
					<xsl:with-param name="type" select="$type"/>
				</xsl:apply-templates>
			</xsl:when>
			<!-- senarioi 2 -->
			<xsl:when test="translate($context/@name, $ABC, $abc) != self::node()/text() and following-sibling::item">
				<xsl:apply-templates select="following-sibling::item[1]" mode="wbt:walk-content">
					<xsl:with-param name="path" select="concat($path, '/', following-sibling::item[1]/text())"/>
					<xsl:with-param name="context" select="$context"/>
					<xsl:with-param name="type" select="$type"/>
				</xsl:apply-templates>
			</xsl:when>
			<!-- scenario 3 -->
			<xsl:when test="translate($context/@name, $ABC, $abc) = self::node()/text() and $context/content[@name[translate(., $ABC, $abc) = current()/following-sibling::item/text()]]">
				<xsl:apply-templates select="following-sibling::item[1]" mode="wbt:walk-content">
					<xsl:with-param name="path" select="concat($path, '/', following-sibling::item[1]/text())"/>
					<xsl:with-param name="context" select="$context/content[@name[translate(., $ABC, $abc) = current()/following-sibling::item/text()]]"/>
					<xsl:with-param name="type" select="$type"/>
				</xsl:apply-templates>
			</xsl:when>
			<xsl:otherwise>
				<xsl:call-template name="wbt:apply-content">
					<xsl:with-param name="context" select="$context | /flx/client/page | /flx/client/content[not(@active='false')]"/>
				</xsl:call-template>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template name="wbt:apply-content">
		<xsl:param name="context"/>
		<xsl:choose>
			<xsl:when test="($context/@authenticateduser = 'true' or $context/@wbt:authenticateduser = 'true') and not($AuthenticatedUser)">
				<xsl:call-template name="protected-content"/>
			</xsl:when>
			<xsl:when test="($context/@loggedonuser = 'true' or $context/@loggedinuser = 'true' or $context/@wbt:loggedonuser = 'true') and not($LoggedOnUser)">
				<xsl:call-template name="protected-content"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:apply-templates select="$context" mode="wbt:apply-content"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	
	<xsl:template match="page | content" mode="wbt:apply-content">
		<xsl:choose>
			<xsl:when test="$wrap-content">
				<xsl:element name="div">
					<xsl:attribute name="id">
						<xsl:choose>
							<xsl:when test="@name">
								<xsl:value-of select="@name"/>
							</xsl:when>
							<xsl:when test="@id">
								<xsl:value-of select="@id"/>
							</xsl:when>
							<xsl:otherwise>
								<xsl:value-of select="generate-id(.)"/>
							</xsl:otherwise>
						</xsl:choose>
					</xsl:attribute>
					<xsl:apply-templates select="*" mode="identity-translate"/>
				</xsl:element>
			</xsl:when>
			<xsl:otherwise>
				<xsl:apply-templates select="*" mode="identity-translate"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>	

	<xsl:template match="content" mode="identity-build-nav">
		<xsl:variable name="nav_link_part1">
			<xsl:text>/</xsl:text>
			<xsl:apply-templates select="ancestor-or-self::*" mode="identity-build-nav.link"/>
		</xsl:variable>
		<xsl:variable name="nav_link_part2">
			<xsl:if test="string(/flx/proj/browser/page/PHANTOM_SUFFIX/item)">
				<xsl:value-of select="concat('/', /flx/proj/browser/page/PHANTOM_SUFFIX/item)"/>
			</xsl:if>
		</xsl:variable>
		<xsl:variable name="nav_link" select="concat($nav_link_part1, $nav_link_part2)"/>
		<xsl:variable name="title">
			<xsl:choose>
				<xsl:when test="@title">
					<xsl:value-of select="@title"/>
				</xsl:when>
				<xsl:when
					test="*[not(namespace-uri() = 'myWebTemplater.1.0' or namespace-uri() = 'mySiteTemplater.1.0' or namespace-uri() = 'myPageTemplater.1.0')][1][name() = 'h1' or name() = 'h2' or name() = 'h3']">
					<xsl:value-of
						select="*[not(namespace-uri() = 'myWebTemplater.1.0' or namespace-uri() = 'mySiteTemplater.1.0' or namespace-uri() = 'myPageTemplater.1.0')][1][name() = 'h1' or name() = 'h2' or name() = 'h3']/text()"
					/>
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="@name"/>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		
		<!-- draw navigation - test for authenticated user, logged on user, and nav choice -->
		<xsl:if test="not(@nav = 'false') and not(@active='false')">
			<xsl:if
				test="
					((ancestor-or-self::*/@loggedonuser = 'true' or ancestor-or-self::*/@wbt:loggedonuser = 'true') and $LoggedOnUser)
					or ((ancestor-or-self::*/@loggedonuser = 'false' or ancestor-or-self::*/@wbt:loggedonuser = 'false'))
					or (not(ancestor-or-self::*/@loggedonuser))">
				<xsl:if
					test="
					((ancestor-or-self::*/@authenticateduser = 'true' or ancestor-or-self::*/@wbt:authenticateduser = 'true') and $AuthenticatedUser)
					or ((ancestor-or-self::*/@authenticateduser = 'false' or ancestor-or-self::*/@wbt:authenticateduser = 'false'))
					or (not(ancestor-or-self::*/@authenticateduser))">
					<li>
						<!-- if content has no html child elements then no link -->
						<xsl:choose>
							<xsl:when test="content and (count(*) = count(content))">
								<xsl:attribute name="class">
									<xsl:value-of select="concat('content-group content-group-', @name)"/>
								</xsl:attribute>
								<xsl:value-of select="$title"/>
							</xsl:when>
							<xsl:otherwise>
								<xsl:choose>
									<xsl:when test="translate(substring-after($nav_link_part1, '/'), $ABC, $abc) = $PageHeirarchyCombined">
										<xsl:attribute name="class">
											<xsl:text>active</xsl:text>
											<xsl:if test="content">
												<xsl:text> content-parent</xsl:text>
											</xsl:if>
										</xsl:attribute>
									</xsl:when>
									<xsl:otherwise>
										<xsl:if test="content">
											<xsl:attribute name="class">
												<xsl:text>content-parent</xsl:text>
											</xsl:attribute>
										</xsl:if>
									</xsl:otherwise>
								</xsl:choose>
								<xsl:call-template name="wbt:build_url">
									<xsl:with-param name="icon-class" select="@icon-class"/>
									<xsl:with-param name="display" select="$title"/>
									<xsl:with-param name="url" select="$nav_link"/>
								</xsl:call-template>
							</xsl:otherwise>
						</xsl:choose>
					</li>
				</xsl:if>
			</xsl:if>
		</xsl:if>
		<xsl:apply-templates select="content" mode="identity-build-nav"/>
	</xsl:template>
	
	<xsl:template match="*" mode="identity-build-nav.link"/>
	<xsl:template match="content[@nav='false' or @active='false']" mode="identity-build-nav.link"/>
	<xsl:template match="page | content" mode="identity-build-nav.link">
		<xsl:if test="not(self::page)">
			<xsl:text>.</xsl:text>
		</xsl:if>
		<xsl:value-of select="@name"/>
	</xsl:template>

	<xsl:template match="page | content" mode="build-crumb">
		<xsl:variable name="nav_link_part1">
			<xsl:text>/</xsl:text>
			<xsl:apply-templates select="ancestor-or-self::*" mode="identity-build-nav.link"/>
		</xsl:variable>
		<xsl:variable name="nav_link_part2">
			<xsl:if test="string(/flx/proj/browser/page/PHANTOM_SUFFIX/item)">
				<xsl:value-of select="concat('/', /flx/proj/browser/page/PHANTOM_SUFFIX/item)"/>
			</xsl:if>
		</xsl:variable>
		<xsl:variable name="nav_link" select="concat($nav_link_part1, $nav_link_part2)"/>
		<xsl:variable name="title">
			<xsl:choose>
				<xsl:when test="@title">
					<xsl:value-of select="@title"/>
				</xsl:when>
				<xsl:when
					test="*[not(namespace-uri() = 'myWebTemplater.1.0' or namespace-uri() = 'mySiteTemplater.1.0' or namespace-uri() = 'myPageTemplater.1.0')][1][name() = 'h1' or name() = 'h2' or name() = 'h3']">
					<xsl:value-of
						select="*[not(namespace-uri() = 'myWebTemplater.1.0' or namespace-uri() = 'mySiteTemplater.1.0' or namespace-uri() = 'myPageTemplater.1.0')][1][name() = 'h1' or name() = 'h2' or name() = 'h3']/text()"
					/>
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="@name"/>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<li>
			<xsl:choose>
				<xsl:when test="self::page and translate(@name, $ABC, $abc) != $BrowserPage[1]">
					<xsl:value-of select="$BrowserPage[1]"/>
					<xsl:text> / </xsl:text>
					<xsl:call-template name="wbt:build_url">
						<xsl:with-param name="display" select="$title"/>
						<xsl:with-param name="url" select="$nav_link"/>
					</xsl:call-template>
				</xsl:when>
				<xsl:otherwise>
					<xsl:choose>
						<xsl:when test="position() = last()">
							<xsl:attribute name="class">active</xsl:attribute>
							<xsl:value-of select="$title"/>
						</xsl:when>
						<xsl:otherwise>
							<xsl:call-template name="wbt:build_url">
								<xsl:with-param name="display" select="$title"/>
								<xsl:with-param name="url" select="$nav_link"/>
							</xsl:call-template>
							<span class="divider">/</span>
							<xsl:value-of select="$title"/>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:otherwise>
			</xsl:choose>
		</li>

	</xsl:template>

	<xsl:template match="LoremIpsum" mode="identity-translate">
		<xsl:variable name="name">
			<xsl:choose>
				<xsl:when test="@p">
					<xsl:value-of select="concat(local-name(), '_', @p)"/>
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="concat(local-name(), '_', '1')"/>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<xsl:variable name="p-index" select="@p"/>

		<xsl:variable name="index">
			<xsl:choose>
				<xsl:when test="count(preceding::LoremIpsum) = 0">
					<xsl:value-of select="1"/>
				</xsl:when>
				<xsl:when test="number($p-index)">
					<xsl:value-of select="count(preceding::LoremIpsum[@p[. = $p-index]]) + 1"/>
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="count(preceding::LoremIpsum[not(@p)]) + 1"/>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<xsl:value-of select="key('wbt:key_Tags', $name)[position() = $index]" disable-output-escaping="yes"/>
	</xsl:template>
	<xsl:template match="HTTP_METHOD" mode="identity-translate">
		<xsl:if test="@verb = $HTTP_METHOD or @verb = '*'">
			<xsl:apply-templates select="*" mode="identity-translate"/>
		</xsl:if>
	</xsl:template>

	<xsl:template match="Redirect" mode="identity-translate">
		<xsl:variable name="time">
			<xsl:choose>
				<xsl:when test="number(@time)">
					<xsl:value-of select="number(@time * 1000)"/>
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="number(1000)"/>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<script type="text/javascript">
            setTimeout(function() { 
            location.href = '<xsl:value-of select="@href | text()"/>'
			}, <xsl:value-of select="$time"/>);
        </script>
	</xsl:template>

	<xsl:template match="LoggedOn | LoggedIn | LoggedInUser | LoggedOut | LoggedOff | LoggedOffUser" mode="identity-translate">
		<xsl:if test="(name() = 'LoggedIn' or name() = 'LoggedOn' or name() = 'LoggedOnUser' or name() = 'LoggedInUser') and $LoggedOnUser">
			<xsl:apply-templates mode="identity-translate"/>
		</xsl:if>
		<xsl:if test="(name() = 'LoggedIn' or name() = 'LoggedOn' or name() = 'LoggedOnUser' or name() = 'LoggedInUser') and not($LoggedOnUser)">
			<xsl:if test="@ShowProtectedMessage = 'true'">
				<xsl:call-template name="protected-content"/>
			</xsl:if>
		</xsl:if>
		<xsl:if test="(name() = 'LoggedOut' or name() = 'LoggedOut' or name() = 'LoggedOff' or name() = 'LoggedOffUser') and not($LoggedOnUser)">
			<xsl:apply-templates mode="identity-translate"/>
		</xsl:if>
	</xsl:template>

	<xsl:template name="protected-content">
		<div class="container">
			<div class="jumbotron">
				<h1>Protected Content</h1>
				<span class="pull-right">|<em> A valid logon to these pages required </em>|</span>
			</div>
		</div>
	</xsl:template>

	<xsl:template match="Authenticated | AuthenticatedUser | NotAuthenticated" mode="identity-translate">
		<xsl:if test="name() = ('Authenticated' or name() = 'AuthenticatedUser') and $AuthenticatedUser">
			<xsl:apply-templates mode="identity-translate"/>
		</xsl:if>
		<xsl:if test="(name() = 'NotAuthenticated' or name() = 'NotAuthenticatedUser') and not($AuthenticatedUser)">
			<xsl:apply-templates mode="identity-translate"/>
		</xsl:if>
	</xsl:template>

	<xsl:template match="TAG" mode="identity-translate">
		<xsl:choose>
			<xsl:when test="count(key('wbt:key_Tags', @name)) &gt; 1">
				<ul>
					<xsl:for-each select="key('wbt:key_Tags', @name)">
						<li>
							<xsl:value-of select="."/>
						</li>
					</xsl:for-each>
				</ul>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="key('wbt:key_Tags', @name)"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template name="wbt:comment">
		<xsl:param name="comment"/>
		<xsl:text>&#10;&#10;</xsl:text>
		<xsl:comment>** <xsl:value-of select="$comment"/> **********</xsl:comment>
		<xsl:text>&#10;</xsl:text>
	</xsl:template>

	<xsl:template name="BackButton" match="BackButton" mode="identity-translate">
		<xsl:param name="href" select="@href"/>
		<xsl:param name="title" select="@title | text()" />
		<xsl:choose>
			<xsl:when test="string($href)">
				<a href="{$href}" class="btn btn-default back-button"><i class="glyphicon glyphicon-chevron-left"/> <xsl:value-of select="$title"/></a>
			</xsl:when>
			<xsl:otherwise>
				<a href="#" onclick="history.back(); return false;" class="btn btn-default back-button"><i class="glyphicon glyphicon-chevron-left"/> Back</a>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template match="IsHuman" mode="identity-translate">
		<xsl:choose>
			<xsl:when test="key('wbt:key_CookieVars', 'goog_ok_user') = 'true' and not(key('wbt:key_Tags', 'ISBot') = 'YES')">
				<xsl:apply-templates mode="identity-translate"/>
			</xsl:when>
			<xsl:otherwise>
				<script>
					<![CDATA[
					function gCaptchaResponse(response) {
					    var waitFunc = function() { 
					    	$('GOOGLE_RECAPTCHA').insert('<input type=\'submit\' name=\'submit-button\' value=\'Click To Continue\'/>');
					    };
					    waitFunc.delay(1.5);
					    $('GOOGLE_RECAPTCHA').insert('<input type=\'hidden\' name=\'redirect\'/>');
					    var redirectpath = location.pathname;
					    if (!location.search.blank()) {
					        redirectpath += '?' + location.search;
					    }
					    if (!location.hash.blank()) {
					        redirectpath += location.hash;
					    } 
					    if(redirectpath.blank()) {
					    	// use fallback redirect path
					    	$('GOOGLE_RECAPTCHA')['redirect'].setValue(encodeURIComponent($('GOOGLE_RECAPTCHA')['redirect2'].getValue()));
					    } else {
					    	$('GOOGLE_RECAPTCHA')['redirect'].setValue(encodeURIComponent(redirectpath));
					    }
					    $('GOOGLE_RECAPTCHA').submit();
					}]]>
				</script>
				<xsl:variable name="site-key" select="@site-key"/>
				<form id="GOOGLE_RECAPTCHA" method="POST" action="/googRecaptcha.aspx" style="margin-left:auto; margin-right:auto; width:294px">
					<div class="g-recaptcha" data-callback="gCaptchaResponse" data-sitekey="{$site-key}"/>
					<p class="lead"> This one time test will insure that you are not a bot. Data integrity is important to us. </p>
					<input type="hidden" name="redirect2">
						<xsl:attribute name="value">
							<xsl:call-template name="get-redirect-path">
								<xsl:with-param name="pattern" select="@redirect-pattern"/>
							</xsl:call-template>
						</xsl:attribute>
					</input>
					<input type="hidden" name="cookie" value="{@cookie}"/>
				</form>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<!-- pattern like: /SomePage/{QueryVar} -->
	<xsl:template name="get-redirect-path">
		<xsl:param name="pattern"/>

		<xsl:choose>
			<!-- recurse until we've exhausted pattern query lookups -->
			<xsl:when test="contains($pattern, '{')">
				<xsl:value-of select="substring-before($pattern, '{')"/>
				<xsl:variable name="parm" select="substring-before(substring-after($pattern, '{'), '}')"/>
				<xsl:value-of select="key('wbt:key_AllVars', $parm)"/>
				<xsl:call-template name="get-redirect-path">
					<xsl:with-param name="pattern" select="substring-after($pattern, '}')"/>
				</xsl:call-template>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="$pattern"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	
	<!-- misc functions -->
	<xsl:template match="link" mode="identity-translate">
		<xsl:element name="a">
			<xsl:attribute name="href">
				<xsl:choose>
					<xsl:when test="@href">
						<xsl:value-of select="@href"/>
					</xsl:when>
					<xsl:otherwise>
						<xsl:value-of select="text()"/>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:attribute>
			<xsl:value-of select="text()"/>
		</xsl:element>
	</xsl:template>
	
	<xsl:template name="JumpOut" match="JumpOut" mode="identity-translate">
		<xsl:param name="href" select="."/>
		<xsl:param name="title" select="@title"/>
		<a>
			<xsl:apply-templates select="@*" mode="identity-translate"/>
			<xsl:attribute name="href">
				<xsl:value-of select="$href"/>
			</xsl:attribute>
			<xsl:attribute name="target">_blank</xsl:attribute>
			<xsl:if test="string($title)">
				<xsl:value-of select="concat($title, ' ')"/>
			</xsl:if>
			<i class="glyphicon glyphicon-new-window"/>
		</a>
	</xsl:template>
	
	<xsl:template match="wbt:ProjSql" mode="identity-translate">
		<div class="row">
			<div class="col-md-12">
				<nav class="navbar navbar-default">
					<div class="collapse navbar-collapse" id="bs-example-navbar-collapse-1">
						<ul class="nav navbar-nav">
							<xsl:apply-templates select="$projSql/*" mode="projsql-nav"/>
						</ul>
					</div>
				</nav>
			</div>
		</div>
	</xsl:template>
	
	<xsl:template match="*" mode="projsql-nav">
		<li class="dropdown">
			<a href="#" class="dropdown-toggle" data-toggle="dropdown" role="button" aria-haspopup="true" aria-expanded="false"><xsl:value-of select="local-name()"/> <span class="caret"></span></a>
			<ul class="dropdown-menu">
				<xsl:apply-templates select="query[command/action='Result']" mode="projsql-nav"/>
				<li class="divider"></li>
				<li class="dropdown-submenu">
					<a href="#" tabindex="-1">Execute</a>
					<ul class="dropdown-menu">
						<xsl:apply-templates select="query[command/action='Scalar'] | query[command/action='NonQuery']" mode="projsql-nav"/>
					</ul>
				</li>
			</ul>
		</li>		
	</xsl:template>
	
	<xsl:template match="query" mode="projsql-nav">
		<li><a href="?wbt_project={local-name(parent::*[1])}&amp;wbt_query={@name}"><xsl:value-of select="@name"/></a></li>
	</xsl:template>
	
	<xsl:template match="@actionx" mode="identity-translate">
		<xsl:attribute name="action">
			<xsl:text>/</xsl:text>
			<xsl:for-each select="$BrowserPage">
				<xsl:value-of select="@title"/>
				<xsl:text>.</xsl:text>
			</xsl:for-each>
			<xsl:value-of select="."/>
		</xsl:attribute>
	</xsl:template>
	
	<xsl:template match="OFF" mode="identity-translate" priority="99"/>
	
	<xsl:template match="@hrefx" name="hrefx" mode="identity-translate">
		<xsl:param name="hrefx"/>
		<xsl:attribute name="href">
			<xsl:text>/</xsl:text>
			<xsl:for-each select="$BrowserPage">
				<xsl:value-of select="@title"/>
				<xsl:text>.</xsl:text>
			</xsl:for-each>
			<xsl:choose>
				<xsl:when test="string($hrefx)">
					<xsl:value-of select="$hrefx"/>					
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="."/>					
				</xsl:otherwise>
			</xsl:choose>
		</xsl:attribute>
	</xsl:template>
	
	<xsl:template match="wbt:*" mode="identity-translate">
		<xsl:element name="{local-name()}" namespace="">
			<xsl:apply-templates select="@*[namespace-uri()='']" mode="identity-translate"/>
			<xsl:choose>
				<xsl:when test="@wbt:query">
					<xsl:apply-templates select="key('wbt:key_Results', @wbt:query)/result/row" mode="wbt:table">
						<xsl:with-param name="template" select="*"/>
					</xsl:apply-templates>
				</xsl:when>
				<xsl:otherwise>
					<xsl:apply-templates select="node()" mode="identity-translate"/>					
				</xsl:otherwise>
			</xsl:choose>
		</xsl:element>
	</xsl:template>
	
	<xsl:template match="tbody[@wbt:query]" mode="identity-translate">
		<xsl:apply-templates select="key('wbt:key_Results', @wbt:query)/result/row" mode="wbt:table">
			<xsl:with-param name="template" select="*"/>
		</xsl:apply-templates>
	</xsl:template>
	
	<xsl:template match="row" mode="wbt:table">		
		<xsl:param name="template"/>
		<xsl:apply-templates select="$template" mode="wbt:table">
			<xsl:with-param name="current" select="current()"/>
		</xsl:apply-templates>
		
	</xsl:template>
	
	<xsl:template match="*" mode="wbt:table">
		<xsl:param name="current" select="/.."/>
		<xsl:variable name="field" select="."/>
		<xsl:copy>
			<xsl:apply-templates select="@*[namespace-uri()='']" mode="identity-translate"/>
			<xsl:apply-templates select="node() | @wbt:*" mode="wbt:table">
				<xsl:with-param name="current" select="$current"/>
			</xsl:apply-templates>
		</xsl:copy>
	</xsl:template>

	<xsl:template match="text()" mode="wbt:table">
		<xsl:value-of select="."/>
	</xsl:template>
	
	<xsl:template match="@wbt:*" mode="wbt:table">
		<xsl:param name="current" select="/.."/>
		<xsl:variable name="field" select="."/>
		<xsl:attribute name="{local-name()}">
			<xsl:value-of select="$current/@*[name()=$field]"/>
		</xsl:attribute>
	</xsl:template>
	
	<xsl:template match="wbt:field" mode="wbt:table">
		<xsl:param name="current" select="/.."/>
		<xsl:variable name="field" select="@name"/>
		<xsl:value-of select="$current/@*[name()=$field]"/>
		<xsl:apply-templates select="text() | *" mode="wbt:table">
			<xsl:with-param name="current" select="$current"/>
		</xsl:apply-templates>
	</xsl:template>
	
	<xsl:template match="wbt:field-attr" mode="wbt:table">
		<xsl:param name="current" select="/.."/>
		<xsl:variable name="field" select="@name"/>
		<xsl:attribute name="data-{$field}">
			<xsl:value-of select="$current/@*[name()=$field]"/>
		</xsl:attribute>
	</xsl:template>
</xsl:stylesheet>

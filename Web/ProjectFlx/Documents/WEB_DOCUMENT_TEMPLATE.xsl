<?xml version="1.0" encoding="utf-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:wbt="myWebTemplater.1.0" xmlns:sbt="mySiteTemplater.1.0" xmlns:pbt="myPageTemplater.1.0" exclude-result-prefixes="xsl wbt sbt pbt">

	<!-- includes -->
	<xsl:include href="WBT_TEMPLATES.xsl"/>
	<xsl:include href="WBT_FORMS.xsl"/>
	<xsl:include href="WBT_TABLES.xsl"/>
	<xsl:include href="WBT_LINKS.xsl"/>
	<xsl:include href="WBT_PAGING.xsl"/>
	<xsl:include href="WBT_Debug.xsl"/>
	<xsl:include href="BS_TWITTER_5.1.xsl"/>

	<xsl:strip-space elements="*"/>
	<!-- IMPORTANT! use of xsl:attribute breaks when white-space introduced -->
	<xsl:output method="html" indent="yes" encoding="utf-8"/>

	<xsl:param name="wrap-content" select="false()"/>
	<xsl:param name="projSql" select="/.."/>

	<!-- INTERNAL PRIVATE PARAMTER VARIABLES -->
	<xsl:param name="DOC_ACTION"/>
	<xsl:param name="DOC_FOLDER"/>
	<xsl:param name="OUT">HTML</xsl:param>
	<xsl:param name="wbt:verbose" select="true()"/>

	<xsl:param name="LoggedOnUser" select="false()"/>
	<xsl:param name="AuthenticatedUser" select="false()"/>
	<xsl:param name="DEBUG" select="false()"/>
	<xsl:param name="is-mobile" select="boolean(key('wbt:capable', 'IsMobile') = 'True')"/>
	<xsl:param name="ShowTimingDebugger" select="false()"/>
	<xsl:param name="source-xml"/>

	<xsl:variable name="wbt:name">WEB_DOCUMENT_TEMPLATE.xsl</xsl:variable>
	<xsl:variable name="UNIX_TIME" select="/flx/proj/browser/@unix_time"/>
	<xsl:variable name="UNIX_TIME2" select="substring-before(/flx/proj/browser/@unix_time, '.')"/>
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
	<xsl:variable name="website-theme.name">
		<xsl:choose>
			<xsl:when test="key('wbt:key_Tags', 'website-theme-override')">
				<xsl:value-of select="key('wbt:key_Tags', 'website-theme-override')"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="key('wbt:key_CookieVars', 'website-theme')"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:variable>
	<xsl:variable name="website-theme" select="/flx/app/Themes/Theme[Name = $website-theme.name]"/>

	<xsl:variable name="HTTPS_IS_ON" select="
			boolean(key('wbt:key_ServerVars', 'HTTPS') = 'on') or
			boolean(key('wbt:key_ServerVars', 'HTTP_X_FORWARDED_PROTO') = 'https')"/>

	<xsl:variable name="protocol">
		<xsl:choose>
			<xsl:when test="$HTTPS_IS_ON">
				<xsl:text>https:</xsl:text>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text>http:</xsl:text>
			</xsl:otherwise>
		</xsl:choose>
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
	<xsl:key name="wbt:key_ProjResults" match="results" use="concat(@project, '.', @name)"/>
	<xsl:key name="wbt:key_Row" match="row" use="ancestor::results/@name"/>
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
				<xsl:call-template name="wbt:body-style"/>
				<xsl:call-template name="wbt:errors"/>
				<xsl:apply-templates select="." mode="wbt:header"/>
				<xsl:call-template name="wbt:body"/>
				<xsl:call-template name="flx-timing"/>
				<xsl:apply-templates select="." mode="wbt:footer"/>
				<xsl:call-template name="Google-Analytics"/>
			</body>
		</html>
	</xsl:template>

	<xsl:template name="wbt:body-class"/>
	<xsl:template name="wbt:body-style"/>
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
												<xsl:value-of select="concat(@Name, ' ', @Exception)"/>
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
		<!-- CSS only -->
		<xsl:choose>
			<xsl:when test="$website-theme">
				<xsl:call-template name="wbt:comment">
					<xsl:with-param name="comment">Custom Theme Applied</xsl:with-param>
				</xsl:call-template>
				<xsl:for-each select="$website-theme/Src">
					<link href="{.}" rel="stylesheet"/>
					<xsl:text>&#10;</xsl:text>
				</xsl:for-each>
			</xsl:when>
			<xsl:otherwise>
				<link href="https://cdn.jsdelivr.net/npm/bootstrap@5.0.0-beta1/dist/css/bootstrap.min.css" rel="stylesheet" integrity="sha384-giJF6kkoqNQ00vy+HMDP7azOuL0xtbfIcaT9wjKHr8RbDVddVHyTfAAsrekwKmP1" crossorigin="anonymous"/>
			</xsl:otherwise>
		</xsl:choose>
		<link href="https://stackpath.bootstrapcdn.com/font-awesome/4.6.3/css/font-awesome.min.css" rel="stylesheet" integrity="sha384-T8Gy5hrqNKT+hzMclPo118YTQO6cYprQmhrYwIiQ/3axmI1hQomh7Ud2hPOy8SP1" crossorigin="anonymous"/>

		<!-- Legacy Bootstrap -->
		<link rel="stylesheet" href="{$protocol}//www3.meetscoresonline.com/ProjectFLX/Documents/bootstrap-legacy.css"/>

		<script type="text/javascript">
            window.wbt = window.wbt || {
            };
            wbt.WebsiteThemeDefaults = {
            };
            wbt.WebsiteThemeDefaults.DefaulltCSS = 'https://cdn.jsdelivr.net/npm/bootstrap@5.0.0-beta1/dist/css/bootstrap.min.css';
            wbt.WebsiteThemeDefaults.DefaultIntegrity = 'sha384-giJF6kkoqNQ00vy+HMDP7azOuL0xtbfIcaT9wjKHr8RbDVddVHyTfAAsrekwKmP1';</script>
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
		<script src="{$protocol}//www3.meetscoresonline.com/ProjectFLX/Bootstrap/ProtoScripty__132132436975227832.js"/>
		<script src="{$protocol}//www3.meetscoresonline.com/ProjectFLX/Bootstrap/protobootstrap__132490265329769314.js"/>

		<!--<script src="/ProjectFLX/Bootstrap/transition_prototype.js"></script>
		<script src="/ProjectFLX/Bootstrap/affix_prototype.js"></script>
		<script src="/ProjectFLX/Bootstrap/alert_prototype.js"></script>
		<script src="/ProjectFLX/Bootstrap/carousel_prototype.js"></script>
		<script src="/ProjectFLX/Bootstrap/button_prototype.js"></script>
		<script src="/ProjectFLX/Bootstrap/collapse_prototype.js"></script>
		<script src="/ProjectFLX/Bootstrap/scrollspy_prototype.js"></script>
		<script src="/ProjectFLX/Bootstrap/dropdown_prototype.js"></script> 	
		<script src="/ProjectFLX/Bootstrap/modal_prototype.js"></script>-->


		<script src="{$protocol}//www3.meetscoresonline.com/ProjectFLX/Documents/event.simulate.js" type="text/javascript"/>
		<script src="{$protocol}//www3.meetscoresonline.com/ProjectFLX/Documents/WBT_SCRIPT.js?v=1.6" type="text/javascript"/>
		<script src="{$protocol}//www3.meetscoresonline.com/ProjectFLX/Documents/WBT_COUNTDOWN.js?v=1.2" type="text/javascript"/>

		<!--Embeded javascript-->
		<xsl:call-template name="wbt:comment">
			<xsl:with-param name="comment">Embeded javascript</xsl:with-param>
		</xsl:call-template>

		<!--Proj Browser Script Item -->
		<xsl:apply-templates select="/" mode="wbt:browser-script-javascript"/>

		<xsl:if test="/flx/app/compilationResult/compiledCode">
			<xsl:call-template name="wbt:comment">
				<xsl:with-param name="comment">GC Compiled javascript</xsl:with-param>
			</xsl:call-template>
			<xsl:for-each select="/flx/app/compilationResult/compiledCode">
				<xsl:text>&#10;</xsl:text>
				<script><xsl:value-of select="." disable-output-escaping="yes"/></script>
			</xsl:for-each>
		</xsl:if>

		<!--Site level javascript-->
		<xsl:call-template name="wbt:comment">
			<xsl:with-param name="comment">Site level javascript</xsl:with-param>
		</xsl:call-template>
		<xsl:call-template name="sbt:javascript"/>

		<!--Page level javascript-->
		<xsl:call-template name="wbt:comment">
			<xsl:with-param name="comment">Page level javascript</xsl:with-param>
		</xsl:call-template>
		<xsl:call-template name="pbt:javascript"/>

		<!--Backend pass-through script-->
		<xsl:call-template name="wbt:comment">
			<xsl:with-param name="comment">Backend pass-through script [001]</xsl:with-param>
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

	<xsl:template match="/" mode="wbt:browser-script-javascript">
		<xsl:for-each select="/flx/proj/browser/page/REQUIRED_SCRIPT/item">
			<xsl:text>&#10;</xsl:text>
			<xsl:variable name="src">
				<xsl:choose>
					<xsl:when test="starts-with(., 'http:')">
						<xsl:value-of select="concat($protocol, substring-after(., 'http:'))"/>
					</xsl:when>
					<xsl:otherwise>
						<xsl:value-of select="."/>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:variable>
			<script src="{$src}"/>
		</xsl:for-each>
		<xsl:for-each select="/flx/proj/browser/page/SCRIPT/item">
			<xsl:sort select="." case-order="lower-first"/>
			<xsl:text>&#10;</xsl:text>
			<xsl:variable name="src">
				<xsl:choose>
					<xsl:when test="starts-with(., 'http:')">
						<xsl:value-of select="concat($protocol, substring-after(., 'http:'))"/>
					</xsl:when>
					<xsl:otherwise>
						<xsl:value-of select="."/>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:variable>
			<script src="{$src}"/>
		</xsl:for-each>
		<xsl:call-template name="wbt:comment">
			<xsl:with-param name="comment">Raw javascript</xsl:with-param>
		</xsl:call-template>
		<xsl:for-each select="/flx/proj/browser/page/RAW_SCRIPT/item">
			<script><xsl:value-of select="." disable-output-escaping="yes"/></script>
		</xsl:for-each>		
	</xsl:template>

	<xsl:template name="wbt:style">
		<xsl:call-template name="wbt:comment">
			<xsl:with-param name="comment">wbt:style</xsl:with-param>
		</xsl:call-template>

		<!-- TODO: map this to CDN repo -->
		<link rel="stylesheet" href="{$protocol}//www3.meetscoresonline.com/ProjectFLX/Documents/WBT_STYLE.css?v=1.6" type="text/css"/>
		<xsl:text>&#10;</xsl:text>
		<link rel="stylesheet" href="{$protocol}//www3.meetscoresonline.com/ProjectFLX/Documents/social-buttons.css" type="text/css"/>
		<xsl:text>&#10;</xsl:text>
		<xsl:call-template name="sbt:style"/>
		<xsl:call-template name="pbt:style"/>
		<xsl:for-each select="/flx/proj/browser/page/STYLE/item">
			<xsl:variable name="src">
				<xsl:choose>
					<xsl:when test="starts-with(., 'http:')">
						<xsl:value-of select="concat($protocol, substring-after(., 'http:'))"/>
					</xsl:when>
					<xsl:otherwise>
						<xsl:value-of select="."/>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:variable>
			<xsl:text>&#10;</xsl:text>
			<link rel="stylesheet" href="{$src}" type="text/css"/>
		</xsl:for-each>
		<xsl:call-template name="sbt:media-style"/>
		<xsl:apply-templates select="/flx/client//pbt:style"/>
	</xsl:template>

	<xsl:template name="sbt:media-style"/>
	<xsl:template name="sbt:style"/>
	<xsl:template name="pbt:style"/>

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
						<xsl:apply-templates select="/flx/client-context/page/content[not(@active = 'false')]" mode="identity-build-nav"/>
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
				<blockquote class="float-end">
					<p>Now get to building that website!</p>
					<small>ProjectFlx</small>
				</blockquote>
			</div>
		</div>
	</xsl:template>

	<xsl:template name="wbt:main"> </xsl:template>

	<xsl:template match="xsl:*[substring-before(@name, ':') = 'wbt']" mode="wbt:transform">
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
	<xsl:template match="xsl:*[contains(@match, ':wbt')]" mode="wbt:transform">
		<a href="#" class="list-group-item">
			<h4>
				<i class="fa fa-th-large" title="Matched Template: {@match}"/>
				<xsl:text> </xsl:text>
				<xsl:value-of select="@match"/>
				<xsl:if test="@mode">
					<small class="float-end">
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
				<xsl:value-of select="concat('?', $Query)"/>
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
		<xsl:param name="hash"/>
		<xsl:param name="onclick"/>
		<xsl:param name="onmouseover"/>
		<xsl:param name="class"/>
		<xsl:param name="target"/>
		<xsl:variable name="q">
			<xsl:if test="string($Query)">
				<xsl:value-of select="concat('?', normalize-space($Query))"/>
			</xsl:if>
		</xsl:variable>
		<xsl:variable name="h">
			<xsl:if test="string($hash)">
				<xsl:value-of select="concat('#', $hash)"/>
			</xsl:if>
		</xsl:variable>
		<a href="{$url}{$q}{$h}">
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
			<xsl:if test="$target">
				<xsl:attribute name="target">
					<xsl:value-of select="$target"/>
				</xsl:attribute>
			</xsl:if>
			<xsl:value-of select="$Display"/>
		</a>
	</xsl:template>

	<!-- convert browsersvars to querystring -->
	<xsl:template name="wbt:AllBrowserVars_ToQueryString">
		<xsl:param name="name"/>
		<xsl:param name="value"/>
		<xsl:apply-templates select="/flx/proj/browser//node()[name() = 'formvars' or name() = 'queryvars']/element" mode="wbt:QueryString">
			<xsl:with-param name="name" select="$name"/>
			<xsl:with-param name="value" select="$value"/>
		</xsl:apply-templates>
		<!-- add name / value if not replaced -->
		<xsl:if test="not(key('wbt:key_FormAndQueryVars', $name)) and string-length($name) &gt; 0">
			<xsl:value-of select="concat('&amp;', $name, '=', $value)"/>
		</xsl:if>
	</xsl:template>

	<xsl:template match="element" mode="wbt:QueryString">
		<xsl:param name="name"/>
		<xsl:param name="value"/>
		<xsl:choose>
			<!-- replace name/value -->
			<xsl:when test="$name = @name">
				<xsl:value-of select="concat($name, '=', $value)"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="concat(@name, '=', .)"/>
			</xsl:otherwise>
		</xsl:choose>
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
				<xsl:value-of select="$val"/>
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

	<xsl:template match="@* | node()" mode="identity-copy" priority="-1">
		<xsl:param name="ignore-name-space" select="false()"/>
		<xsl:if test="$ignore-name-space or not(string(namespace-uri()))">
			<xsl:copy>
				<xsl:apply-templates select="@* | node()" mode="identity-copy"/>
			</xsl:copy>
		</xsl:if>
	</xsl:template>


	<xsl:template match="*" mode="identity-translate">
		<xsl:if test="$wbt:verbose">
			<xsl:comment>identity-translate [*] - <xsl:value-of select="$wbt:name"/></xsl:comment>
		</xsl:if>
		<xsl:choose>
			<xsl:when test="@valid-on">
				<xsl:call-template name="valid-on"/>
			</xsl:when>
			<xsl:when test="@keep-as">
				<xsl:call-template name="identity-template.keep-as"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:call-template name="identity-translate"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template name="valid-on">
		<xsl:param name="valid-on" select="@valid-on"/>
		<xsl:param name="keep-as" select="@keep-as"/>
		<xsl:param name="valid-on-title" select="@valid-on-title"/>
		<xsl:param name="valid-on-description" select="@valid-on-description"/>
		<xsl:param name="valid-on-hidden" select="false()"/>
		<xsl:param name="valid-on-size">large</xsl:param>
		<xsl:param name="content" select="/.."/>

		<xsl:if test="$wbt:verbose">
			<xsl:comment>identity-translate [valid-on] - <xsl:value-of select="$wbt:name"/></xsl:comment>
		</xsl:if>
		<div class="valid-on-{$valid-on-size} mb-2">
			<xsl:choose>
				<xsl:when test="number($valid-on) &lt; number($UNIX_TIME)">
					<xsl:choose>
						<xsl:when test="$keep-as">
							<xsl:call-template name="identity-template.keep-as"/>
						</xsl:when>
						<xsl:when test="$content">
							<xsl:apply-templates select="$content" mode="identity-translate"/>
						</xsl:when>
						<xsl:otherwise>
							<xsl:call-template name="identity-translate"/>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:when>
				<xsl:otherwise>
					<xsl:if test="not($valid-on-hidden)">
						<div class="bg-light rounded-lg">
							<h1>
								<xsl:choose>
									<xsl:when test="$valid-on-title">
										<xsl:value-of select="$valid-on-title"/>
									</xsl:when>
									<xsl:otherwise>
										<xsl:text>Time Sensitive Material</xsl:text>
									</xsl:otherwise>
								</xsl:choose>
							</h1>
							<xsl:if test="$valid-on-description">
								<p class="lead">
									<xsl:value-of select="$valid-on-description"/>
								</p>
							</xsl:if>
							<hr class="my-4"/>
							<div data-unix-time="{$UNIX_TIME}" data-valid-on="{$valid-on}" class="wbt-countdown countdown-days countdown-hours countdown-minutes countdown-seconds "/>
						</div>
						<div class="clearb"/>
					</xsl:if>
				</xsl:otherwise>
			</xsl:choose>
		</div>
	</xsl:template>

	<xsl:template name="identity-template.keep-as">
		<xsl:if test="$wbt:verbose">
			<xsl:comment>identity-translate [keep-as] keep-as: <xsl:value-of select="@keep-as"/> is-mobile: <xsl:value-of select="$is-mobile"/> - <xsl:value-of select="$wbt:name"/></xsl:comment>
		</xsl:if>
		<xsl:choose>
			<xsl:when test="local-name() = 'mobile' or local-name() = 'Mobile'">
				<xsl:if test="$is-mobile">
					<xsl:element name="{@keep-as}">
						<xsl:attribute name="id">
							<xsl:value-of select="local-name()"/>
						</xsl:attribute>
						<xsl:apply-templates select="@*" mode="identity-translate"/>
						<xsl:apply-templates select="node()" mode="identity-translate"/>
					</xsl:element>
				</xsl:if>
			</xsl:when>
			<xsl:otherwise>
				<xsl:element name="{@keep-as}">
					<xsl:attribute name="id">
						<xsl:value-of select="local-name()"/>
					</xsl:attribute>
					<xsl:apply-templates select="@*" mode="identity-translate"/>
					<xsl:apply-templates select="node()" mode="identity-translate"/>
				</xsl:element>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template match="@* | node()" name="identity-translate" mode="identity-translate" priority="-1">
		<xsl:param name="ignore-name-space" select="false()"/>
		<xsl:if test="$ignore-name-space or not(string(namespace-uri()))">
			<xsl:choose>
				<xsl:when test="self::*">
					<xsl:element name="{local-name()}">
						<xsl:apply-templates select="@*" mode="identity-translate"/>
						<xsl:apply-templates select="node()" mode="identity-translate"/>
					</xsl:element>
				</xsl:when>
				<xsl:otherwise>
					<xsl:copy>
						<xsl:apply-templates select="@*" mode="identity-translate"/>
						<xsl:apply-templates select="node()" mode="identity-translate"/>
					</xsl:copy>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:if>
	</xsl:template>

	<xsl:template match="sbt:* | wbt:* | pbt:*" mode="identity-translate"/>

	<xsl:template match="pbt:javascript | pbt:style" priority="1" mode="identity-translate"/>
	<xsl:template match="pbt:javascript | pbt:style" priority="1"/>

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
				<li class="breadcrumb-item">
					<xsl:choose>
						<xsl:when test="position() = last()">
							<xsl:attribute name="class">breadcrumb-item active</xsl:attribute>
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
		<xsl:param name="path" select="$BrowserPage[1]"/>
		<xsl:if test="$wbt:verbose">
			<xsl:comment>wbt:walk-content - <xsl:value-of select="$wbt:name"/></xsl:comment>
		</xsl:if>
		<xsl:apply-templates select="$path" mode="wbt:walk-content">
			<xsl:with-param name="type" select="$type"/>
		</xsl:apply-templates>
	</xsl:template>

	<xsl:template match="item" mode="wbt:walk-content">
		<xsl:param name="type">content</xsl:param>
		<xsl:param name="path" select="text()"/>
		<xsl:param name="context" select="/flx/client/*[local-name() = 'page' or local-name() = 'content'][translate(@name, $ABC, $abc) = current()/text()][not(@active = 'false')]"/>
		<xsl:param name="title">
			<xsl:if test="$wbt:verbose">
				<xsl:comment><xsl:value-of select="$wbt:inc-name"/></xsl:comment>
			</xsl:if>
			<xsl:choose>
				<xsl:when test="$context/@title">
					<xsl:value-of select="$context/@title"/>
				</xsl:when>
				<xsl:when test="$context/*[not(namespace-uri() = 'myWebTemplater.1.0' or namespace-uri() = 'mySiteTemplater.1.0' or namespace-uri() = 'myPageTemplater.1.0')][1][name() = 'h1' or name() = 'h2' or name() = 'h3']">
					<xsl:value-of select="$context/*[not(namespace-uri() = 'myWebTemplater.1.0' or namespace-uri() = 'mySiteTemplater.1.0' or namespace-uri() = 'myPageTemplater.1.0')][1][name() = 'h1' or name() = 'h2' or name() = 'h3']/text()"/>
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="text()"/>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:param>
		<xsl:choose>
			<xsl:when test="$type = 'breadcrumb'">
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
			</xsl:when>
			<!-- scenario 1 -->
			<xsl:when test="not($context) and following-sibling::item[1]">
				<xsl:if test="$wbt:verbose">
					<xsl:comment>wbt:walk-content [scenario 1] - <xsl:value-of select="$wbt:name"/></xsl:comment>
				</xsl:if>
				<xsl:apply-templates select="following-sibling::item[1]" mode="wbt:walk-content">
					<xsl:with-param name="path" select="concat($path, '/', following-sibling::item[1]/text())"/>
					<xsl:with-param name="type" select="$type"/>
				</xsl:apply-templates>
			</xsl:when>
			<!-- senarioi 2 -->
			<xsl:when test="translate($context/@name, $ABC, $abc) != self::node()/text() and following-sibling::item">
				<xsl:if test="$wbt:verbose">
					<xsl:comment>wbt:walk-content [scenario 2] - <xsl:value-of select="$wbt:name"/></xsl:comment>
				</xsl:if>
				<xsl:apply-templates select="following-sibling::item[1]" mode="wbt:walk-content">
					<xsl:with-param name="path" select="concat($path, '/', following-sibling::item[1]/text())"/>
					<xsl:with-param name="context" select="$context"/>
					<xsl:with-param name="type" select="$type"/>
				</xsl:apply-templates>
			</xsl:when>
			<!-- scenario 3 -->
			<xsl:when test="translate($context/@name, $ABC, $abc) = self::node()/text() and $context/content[@name[translate(., $ABC, $abc) = current()/following-sibling::item/text()]]">
				<xsl:if test="$wbt:verbose">
					<xsl:comment>wbt:walk-content [scenario 3] - <xsl:value-of select="$wbt:name"/></xsl:comment>
				</xsl:if>
				<xsl:apply-templates select="following-sibling::item[1]" mode="wbt:walk-content">
					<xsl:with-param name="path" select="concat($path, '/', following-sibling::item[1]/text())"/>
					<xsl:with-param name="context" select="$context/content[@name[translate(., $ABC, $abc) = current()/following-sibling::item/text()]]"/>
					<xsl:with-param name="type" select="$type"/>
				</xsl:apply-templates>
			</xsl:when>
			<!-- scenario 4 -->
			<xsl:otherwise>
				<xsl:if test="$wbt:verbose">
					<xsl:comment>wbt:walk-content [scenario 4] - <xsl:value-of select="$wbt:name"/></xsl:comment>
				</xsl:if>
				<xsl:call-template name="wbt:apply-content">
					<xsl:with-param name="context" select="$context | /flx/client/page | /flx/client/content[not(@active = 'false')]"/>
				</xsl:call-template>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template name="wbt:apply-content">
		<xsl:param name="context"/>
		<xsl:choose>
			<xsl:when test="($context/@authenticateduser = 'true' or $context/@wbt:authenticateduser = 'true') and not($AuthenticatedUser)">
				<xsl:if test="$wbt:verbose">
					<xsl:comment>wbt:apply-content [protected-content 1] - <xsl:value-of select="$wbt:name"/></xsl:comment>
				</xsl:if>
				<xsl:call-template name="protected-content"/>
			</xsl:when>
			<xsl:when test="($context/@loggedonuser = 'true' or $context/@loggedinuser = 'true' or $context/@wbt:loggedonuser = 'true') and not($LoggedOnUser)">
				<xsl:if test="$wbt:verbose">
					<xsl:comment>wbt:apply-content [protected-content 2] - <xsl:value-of select="$wbt:name"/></xsl:comment>
				</xsl:if>
				<xsl:call-template name="protected-content"/>
			</xsl:when>
			<xsl:when test="$context">
				<xsl:if test="$wbt:verbose">
					<xsl:comment>wbt:apply-content [<xsl:value-of select="concat(local-name($context),'[@name=',$context/@name, ']')"/>] - <xsl:value-of select="$wbt:name"/></xsl:comment>
				</xsl:if>
				<xsl:apply-templates select="$context" mode="wbt:apply-content"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:if test="$wbt:verbose">
					<xsl:comment>wbt:apply-content [otherwise] - <xsl:value-of select="$wbt:name"/></xsl:comment>
				</xsl:if>
				<xsl:call-template name="wbt:no-content"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	
	<xsl:template name="wbt:no-content">
		<xsl:if test="$wbt:verbose">
			<xsl:comment>Template wbt:no-content - <xsl:value-of select="$wbt:name"/></xsl:comment>
		</xsl:if>
		<div class="m-5 p-5" id="WBT_NO_CONTENT">
			<h1 class="m-5 p-5 display-3 text-center text-warning text-muted bold text-uppercase">no content found</h1>
		</div>
	</xsl:template>

	<xsl:template match="page | content" mode="wbt:apply-content">
		<xsl:if test="$wbt:verbose">
			<xsl:comment>wbt:apply-content [page | content - <xsl:value-of select="local-name()"/> ] - <xsl:value-of select="$wbt:name"/></xsl:comment>
		</xsl:if>
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

	<xsl:template match="content" mode="identity-build-nav" name="identity-build-nav">
		<xsl:param name="is-buttons" select="false() or boolean(@wbt:nav-style = 'buttons')"/>
		<xsl:param name="class"/>
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
				<xsl:otherwise>
					<xsl:value-of select="@name"/>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>

		<!-- draw navigation - test for authenticated user, logged on user, and nav choice -->
		<xsl:variable name="test1">
			<xsl:call-template name="wbt:content-loggedon"/>
			<xsl:call-template name="wbt:content-authenticated"/>/ </xsl:variable>
		<xsl:variable name="valid-for-content" select="not(contains($test1, 'false'))"/>

		<xsl:if test="not(ancestor-or-self::node()[@nav = 'false']) and not(@active = 'false') and $valid-for-content">
			<xsl:choose>
				<xsl:when test="$is-buttons">
					<xsl:variable name="class.2">
						<xsl:choose>
							<xsl:when test="translate(substring-after($nav_link_part1, '/'), $ABC, $abc) = $PageHeirarchyCombined">
								<xsl:value-of select="concat('btn btn-primary ', $class)"/>
							</xsl:when>
							<xsl:otherwise>
								<xsl:value-of select="concat('btn btn-secondary ', $class)"/>
							</xsl:otherwise>
						</xsl:choose>
					</xsl:variable>
					<xsl:call-template name="wbt:build_url">
						<xsl:with-param name="icon-class" select="@icon-class"/>
						<xsl:with-param name="class" select="$class.2"/>
						<xsl:with-param name="display" select="$title"/>
						<xsl:with-param name="url" select="$nav_link"/>
					</xsl:call-template>
				</xsl:when>
				<xsl:otherwise>
					<li class="nav-item">
						<!-- if content has no html child elements then no link -->
						<xsl:choose>
							<xsl:when test="@flx_empty_content = 'true'">
								<span class="nav-link" title="No Content">
									<xsl:value-of select="$title"/>
								</span>
							</xsl:when>
							<xsl:otherwise>
								<xsl:choose>
									<xsl:when test="translate(substring-after($nav_link_part1, '/'), $ABC, $abc) = $PageHeirarchyCombined">
										<xsl:attribute name="class">
											<xsl:text>active temp_5</xsl:text>
											<xsl:if test="content">
												<xsl:text> content-parent</xsl:text>
											</xsl:if>
										</xsl:attribute>
									</xsl:when>
									<xsl:otherwise>
										<xsl:if test="content">
											<xsl:attribute name="class">
												<xsl:text>content-parent temp_6</xsl:text>
											</xsl:attribute>
										</xsl:if>
									</xsl:otherwise>
								</xsl:choose>
								<xsl:call-template name="wbt:build_url">
									<xsl:with-param name="icon-class" select="@icon-class"/>
									<xsl:with-param name="class" select="$class"/>
									<xsl:with-param name="display" select="$title"/>
									<xsl:with-param name="url" select="$nav_link"/>
								</xsl:call-template>
							</xsl:otherwise>
						</xsl:choose>
					</li>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:if>

		<xsl:apply-templates select="content" mode="identity-build-nav">
			<xsl:with-param name="is-buttons" select="$is-buttons"/>
			<xsl:with-param name="class" select="$class"/>
		</xsl:apply-templates>
	</xsl:template>

	<xsl:template match="*" mode="identity-build-nav.link"/>
	<xsl:template match="content[@nav = 'false' or @active = 'false']" mode="identity-build-nav.link"/>
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
				<xsl:when test="*[not(namespace-uri() = 'myWebTemplater.1.0' or namespace-uri() = 'mySiteTemplater.1.0' or namespace-uri() = 'myPageTemplater.1.0')][1][name() = 'h1' or name() = 'h2' or name() = 'h3']">
					<xsl:value-of select="*[not(namespace-uri() = 'myWebTemplater.1.0' or namespace-uri() = 'mySiteTemplater.1.0' or namespace-uri() = 'myPageTemplater.1.0')][1][name() = 'h1' or name() = 'h2' or name() = 'h3']/text()"/>
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
	<xsl:template match="HTTP_GET | HTTP_POST | HTTP_METHOD" mode="identity-translate">
		<xsl:choose>
			<xsl:when test="local-name() = 'HTTP_METHOD' and (@verb = $HTTP_METHOD or @verb = '*')">
				<xsl:apply-templates select="*" mode="identity-translate"/>
			</xsl:when>
			<xsl:when test="local-name() = 'HTTP_POST' and $HTTP_METHOD = 'POST'">
				<xsl:apply-templates select="*" mode="identity-translate"/>
			</xsl:when>
			<xsl:when test="local-name() = 'HTTP_GET' and $HTTP_METHOD = 'GET'">
				<xsl:apply-templates select="*" mode="identity-translate"/>
			</xsl:when>
		</xsl:choose>
	</xsl:template>
	
	<xsl:template match="Appear" mode="identity-translate">
		<xsl:variable name="id" select="generate-id()"/>
		<xsl:variable name="delay">
			<xsl:choose>
				<xsl:when test="@time"><xsl:value-of select="@time"/></xsl:when>
				<xsl:otherwise>0</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<div style="display:none" id="{$id}">
			<xsl:apply-templates mode="identity-translate"/>
		</div>
		<script type="text/javascript">
			
			<xsl:text>&#10;(function() { $('</xsl:text>
			<xsl:value-of select="$id"/>
			<xsl:text>').appear(); }).delay(</xsl:text>
			<xsl:value-of select="@time"/>
			<xsl:text>);&#10;</xsl:text>
		</script>
	</xsl:template>

	<xsl:template match="RedirectForm" name="RedirectForm" mode="identity-translate">
		<xsl:param name="time" select="@time"/>
		<xsl:param name="href" select="@href | text()"/>
		<xsl:variable name="timex">
			<xsl:choose>
				<xsl:when test="number($time)">
					<xsl:value-of select="$time * 1000"/>
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="1000"/>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<xsl:variable name="id" select="concat('__', generate-id())"/>
		<form id="{$id}" method="post" action="{Action}">
			<xsl:apply-templates select="*" mode="identity-translate"/>
		</form>
		<script type="text/javascript">
			<xsl:text>var form = 
			setTimeout(function() {
			</xsl:text>
				<xsl:text>$('</xsl:text>
				<xsl:value-of select="$id"/>
				<xsl:text>').submit();</xsl:text>

        },<xsl:value-of select="$timex"/>);
		</script>
	</xsl:template>
	<xsl:template match="Action[ancestor::RedirectForm]" mode="identity-translate"/>
	
	<xsl:template match="Redirect" name="Redirect" mode="identity-translate">
		<xsl:param name="time" select="@time"/>
		<xsl:param name="href" select="@href | text()"/>

		<xsl:variable name="timex">
			<xsl:choose>
				<xsl:when test="number($time)">
					<xsl:value-of select="$time * 1000"/>
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="1000"/>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>

		<script type="text/javascript">
            setTimeout(function () {
                location.href = '<xsl:value-of select="$href"/>'
			}, <xsl:value-of select="$timex"/>);
		</script>
	</xsl:template>

	<xsl:template match="UserRoles" mode="identity-translate"/>
	<xsl:template match="LoggedOn | LoggedIn | LoggedInUser | LoggedOut | LoggedOff | LoggedOffUser" mode="identity-translate">
		<xsl:if test="(name() = 'LoggedIn' or name() = 'LoggedOn' or name() = 'LoggedOnUser' or name() = 'LoggedInUser') and $LoggedOnUser">
			<xsl:choose>
				<xsl:when test="UserRoles/Role">
					<xsl:choose>
						<xsl:when test="UserRoles/Role and UserRoles/Role[. = /flx/app/user-roles/role]">
							<xsl:apply-templates mode="identity-translate"/>
						</xsl:when>
						<xsl:otherwise>
							<xsl:call-template name="protected-content"/>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:when>
				<xsl:otherwise>
					<xsl:apply-templates mode="identity-translate"/>
				</xsl:otherwise>
			</xsl:choose>
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
				<span class="float-end">|<em> A valid logon to these pages required </em>|</span>
			</div>
		</div>
	</xsl:template>

	<xsl:template match="Authenticated | AuthenticatedUser | NotAuthenticated" mode="identity-translate">
		<xsl:if test="(name() = 'Authenticated' or name() = 'AuthenticatedUser') and $AuthenticatedUser">
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
		<xsl:if test="$wbt:verbose">
			<xsl:text>&#10;&#10;</xsl:text>
			<xsl:comment>** <xsl:value-of select="$comment"/> **********</xsl:comment>
			<xsl:text>&#10;</xsl:text>
		</xsl:if>
	</xsl:template>

	<xsl:template name="BackButton" match="BackButton" mode="identity-translate">
		<xsl:param name="href" select="@href"/>
		<xsl:param name="title" select="@title | text()"/>
		<xsl:choose>
			<xsl:when test="string($href)">
				<a href="{$href}" class="btn btn-secondary back-button">
					<i class="fa fa-chevron-left"/>
					<xsl:value-of select="$title"/>
				</a>
			</xsl:when>
			<xsl:otherwise>
				<a href="#" onclick="history.back(); return false;" class="btn btn-secondary back-button"><i class="fa fa-chevron-left"/> Back</a>
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
				<script src="https://www.google.com/recaptcha/api.js"/>
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

	<xsl:template name="JumpOut" match="JumpOut | PopOut" mode="identity-translate">
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
			<i class="fa fa-external-link"/>
		</a>
	</xsl:template>

	<xsl:template match="wbt:ProjSql" mode="identity-translate">
		<nav class="navbar navbar-expand navbar-dark bg-dark">
			<div class="collapse navbar-collapse">
				<ul class="navbar-nav">
					<xsl:apply-templates select="$projSql/descendant-or-self::projectSql/*" mode="projsql-nav"/>
				</ul>
			</div>
		</nav>
	</xsl:template>

	<xsl:template match="KeyHole | Keyhole | keyhole" mode="identity-translate">
		<iframe name="keyhole" src="about:blank" width="0" height="0" style="border:none"/>
	</xsl:template>

	<xsl:template match="*" mode="projsql-nav">
		<li class="nav-item dropdown">
			<a href="#" class="nav-link dropdown-toggle" data-toggle="dropdown" role="button" aria-haspopup="true" aria-expanded="false">
				<xsl:value-of select="local-name()"/>
				<span class="caret"/>
			</a>
			<ul class="dropdown-menu">
				<xsl:apply-templates select="query[command/action = 'Result']" mode="projsql-nav">
					<xsl:sort select="@name"/>
				</xsl:apply-templates>
				<li class="dropdown-divider"/>
				<li class="dropdown-item dropdown-submenu">
					<a class="dropdown-toggle" data-toggle="dropdown" href="#" tabindex="-1">Execute</a>
					<ul class="dropdown-menu">
						<xsl:apply-templates select="query[command/action = 'Scalar'] | query[command/action = 'NonQuery']" mode="projsql-nav">
							<xsl:sort select="@name"/>
						</xsl:apply-templates>
					</ul>
				</li>
			</ul>
		</li>
	</xsl:template>

	<xsl:template match="query" mode="projsql-nav">
		<li class="dropdown-item">
			<a href="?wbt_project={local-name(parent::*[1])}&amp;wbt_query={@name}">
				<xsl:value-of select="@name"/>
			</a>
		</li>
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
			<xsl:apply-templates select="@*[namespace-uri() = '']" mode="identity-translate"/>
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
			<xsl:apply-templates select="@*[namespace-uri() = '']" mode="identity-translate"/>
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
			<xsl:value-of select="$current/@*[name() = $field]"/>
		</xsl:attribute>
	</xsl:template>

	<xsl:template match="wbt:field" mode="wbt:table">
		<xsl:param name="current" select="/.."/>
		<xsl:variable name="field" select="@name"/>
		<xsl:value-of select="$current/@*[name() = $field]"/>
		<xsl:apply-templates select="text() | *" mode="wbt:table">
			<xsl:with-param name="current" select="$current"/>
		</xsl:apply-templates>
	</xsl:template>

	<xsl:template match="wbt:field-attr" mode="wbt:table">
		<xsl:param name="current" select="/.."/>
		<xsl:variable name="field" select="@name"/>
		<xsl:attribute name="data-{$field}">
			<xsl:value-of select="$current/@*[name() = $field]"/>
		</xsl:attribute>
	</xsl:template>

	<xsl:template name="flx-timing">
		<xsl:if test="$ShowTimingDebugger">
			<xsl:apply-templates select="/flx/app/TimingDebugger[*]" mode="flx-timing"/>
		</xsl:if>
	</xsl:template>

	<xsl:template match="TimingDebugger" mode="flx-timing">
		<div class="container-fluid">
			<table class="table table-info border">
				<caption>Timing Debugger</caption>
				<thead>
					<tr>
						<th>Name</th>
						<th>Time</th>
					</tr>
				</thead>
				<tbody>
					<xsl:apply-templates mode="flx-timing"/>
				</tbody>
			</table>
		</div>
	</xsl:template>

	<xsl:template match="Name | ExecutionTime" mode="flx-timing"/>

	<xsl:template match="TimingGroup" mode="flx-timing">
		<tr class="table-info">
			<th>
				<xsl:value-of select="@Name"/>
			</th>
			<th>
				<xsl:value-of select="@ExecutionTime"/>
			</th>
		</tr>
		<xsl:apply-templates mode="flx-timing"/>
	</xsl:template>

	<xsl:template match="Timing" mode="flx-timing">
		<xsl:if test="not(position() = 1)">
			<tr class="table-light">
				<xsl:choose>
					<xsl:when test="ExecutionTime &gt; 2000">
						<xsl:attribute name="class">table-danger</xsl:attribute>
					</xsl:when>
					<xsl:when test="ExecutionTime &gt; 750">
						<xsl:attribute name="class">table-warning</xsl:attribute>
					</xsl:when>
				</xsl:choose>
				<td>
					<xsl:value-of select="Name"/>
				</td>
				<td>
					<xsl:value-of select="ExecutionTime"/>
				</td>
			</tr>
		</xsl:if>
	</xsl:template>

	<xsl:template name="wbt:content-loggedon">
		<xsl:choose>
			<xsl:when
				test="
					((ancestor-or-self::*/@loggedonuser = 'true' or ancestor-or-self::*/@wbt:loggedonuser = 'true') and $LoggedOnUser)
					or ((ancestor-or-self::*/@loggedonuser = 'false' or ancestor-or-self::*/@wbt:loggedonuser = 'false'))
					or (not(ancestor-or-self::*/@loggedonuser))">
				<xsl:text>true</xsl:text>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text>false</xsl:text>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template name="wbt:content-authenticated">
		<xsl:choose>
			<xsl:when
				test="
					((ancestor-or-self::*/@authenticateduser = 'true' or ancestor-or-self::*/@wbt:authenticateduser = 'true') and $AuthenticatedUser)
					or ((ancestor-or-self::*/@authenticateduser = 'false' or ancestor-or-self::*/@wbt:authenticateduser = 'false'))
					or (not(ancestor-or-self::*/@authenticateduser))">
				<xsl:text>true</xsl:text>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text>false</xsl:text>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

</xsl:stylesheet>

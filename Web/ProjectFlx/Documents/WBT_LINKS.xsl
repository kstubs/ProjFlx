<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	xmlns:wbt="myWebTemplater.1.0">

<xsl:param name="LINKS" select="/.."/>
<xsl:variable name="SHOWLINKS">True</xsl:variable>

<!-- DISPLAY LINKS ACCORDING TO LINK FILE -->
<xsl:template name="wbt:LINKS_MAIN">
	<xsl:choose>
    	<xsl:when test="$SHOWLINKS='True'">
            <xsl:variable name="current_L" select="$LINKS/L"/>
            <!-- determine if we are displaying links horizontally or vertically -->

            <xsl:choose>
            	<xsl:when test="$LINKS//@displayvertical='true'">
                    <xsl:apply-templates select="$current_L/LINKS[@folder=$DOC_FOLDER]" mode="vertical"><xsl:with-param name="CLASS">links_folder</xsl:with-param></xsl:apply-templates>
                    <xsl:apply-templates select="$current_L/LINKS[@script=$DOC_ACTION]" mode="vertical"><xsl:with-param name="CLASS">links_script</xsl:with-param></xsl:apply-templates>
            	</xsl:when>
            	<xsl:otherwise>
                    <xsl:apply-templates select="$current_L/LINKS[@folder=$DOC_FOLDER]" mode="horizontal"><xsl:with-param name="CLASS">links_folder</xsl:with-param></xsl:apply-templates>
                    <xsl:apply-templates select="$current_L/LINKS[@script=$DOC_ACTION]" mode="horizontal"><xsl:with-param name="CLASS">links_script</xsl:with-param></xsl:apply-templates>
            	</xsl:otherwise>
            </xsl:choose>
    	</xsl:when>
    	<xsl:otherwise>&#160;</xsl:otherwise>
    </xsl:choose>
</xsl:template>

<xsl:template match="LINKS" mode="horizontal">
<xsl:param name="CLASS"/>
	<table class="{$CLASS}" border="0" cellpadding="0" cellspacing="0">
    	<tr>
        	<xsl:apply-templates select="LINK"/>
        </tr>
    </table>
</xsl:template>

<xsl:template match="LINKS" mode="vertical">
<xsl:param name="CLASS"/>
	<table class="{$CLASS}">
    	<xsl:apply-templates select="LINK" mode="vertical"/>
    </table>
</xsl:template>

<xsl:template match="LINK">

	<xsl:variable name="folder"><xsl:value-of select="parent::*/@folder"/></xsl:variable>
	<!-- determine if we are showing a graphical link -->
    	<td>
			<!-- active link? -->
        	<xsl:if test="$DOC_ACTION=@href or $DOC_ACTION=concat(parent::*/@folder,'/',@href)">
            	<xsl:call-template name="addclass_active_link"/>
        	</xsl:if>
    	
        <xsl:choose>
        	<xsl:when test="img">
            	<a href="{@href}"><img alt="{img/@alt}" width="{img/@width}" height="{img/@height}" border="0">
                <!-- determine if active link -->
                <xsl:choose>
                	<xsl:when test="$DOC_ACTION=@href or $DOC_ACTION=concat(parent::*/@folder,'/',@href)">
                    	<xsl:attribute name="src"><xsl:value-of select="img/@src2"/></xsl:attribute>
                	</xsl:when>
                	<xsl:otherwise>
                    	<xsl:attribute name="src"><xsl:value-of select="img/@src"/></xsl:attribute>
                	</xsl:otherwise>
                </xsl:choose>
                </img></a>
        	</xsl:when>
          <xsl:when test="@onclick">
            <a href="{@href}" onclick="{@onclick}">
              <xsl:value-of select="@text"/>
            </a>
            <xsl:if test="not (position()=last())">
              <xsl:call-template name="LINKS_SEPERATOR"/>
            </xsl:if>
          </xsl:when>
        	<xsl:otherwise>
            	<a href="{@href}"><xsl:value-of select="@text"/></a>
                <xsl:if test="not (position()=last())">
                	<xsl:call-template name="LINKS_SEPERATOR"/>
                </xsl:if>
        	</xsl:otherwise>
        </xsl:choose>
        <!-- add final seperator -->
        <!-- NOTE:  This works currently, but may need to give user ability to ignore last call of seperator -->
        <xsl:if test="position()=last()">
			<xsl:call-template name="LINKS_SEPERATOR"/>
        </xsl:if>
		</td>
</xsl:template>

<!-- give user chance to add class to the active link td element -->
<xsl:template name="addclass_active_link"/>

<xsl:template match="LINK" mode="vertical">

	<xsl:variable name="folder"><xsl:value-of select="parent::*/@folder"/></xsl:variable>
	<!-- determine if we are showing a graphical link -->
    	<tr><td>
        <xsl:choose>
        	<xsl:when test="img"><a href="{@href}"><img alt="{img/@alt}" width="{img/@width}" height="{img/@height}" border="0">
                <!-- determine if active link -->
                <xsl:choose>
                	<xsl:when test="$DOC_ACTION=@href or $DOC_ACTION=concat(parent::*/@folder,'/',@href)">
                    	<xsl:attribute name="src"><xsl:value-of select="img/@src2"/></xsl:attribute>
                	</xsl:when>
                	<xsl:otherwise>
                    	<xsl:attribute name="src"><xsl:value-of select="img/@src"/></xsl:attribute>
                	</xsl:otherwise>
                </xsl:choose>
                </img></a>
        	</xsl:when>
        	<xsl:otherwise>
            	<a href="{@href}"><xsl:value-of select="@text"/></a>
                <xsl:if test="not (position()=last())">
                	<tr><xsl:call-template name="LINKS_SEPERATOR"/></tr>
                </xsl:if>
        	</xsl:otherwise>
        </xsl:choose>
		</td></tr>
</xsl:template>

<xsl:template name="LINKS_SEPERATOR"/>






</xsl:stylesheet>